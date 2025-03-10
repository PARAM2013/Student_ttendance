using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models.Logging;
using System.Text.RegularExpressions;

namespace Student_Attendance.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public LoggingService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task LogActivityAsync(string action, string entityType, string? entityId = null,
            string? details = null, string? oldValue = null, string? newValue = null,
            string? module = null, bool isSuccess = true, string? status = null)
        {
            var log = new ActivityLog
            {
                Timestamp = DateTime.UtcNow,
                LogLevel = "Information",
                LogType = "Activity",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                OldValue = oldValue,
                NewValue = newValue,
                Module = module,
                IsSuccess = isSuccess,
                Status = status
            };

            await EnrichLogWithContextData(log);
            await _context.ActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogErrorAsync(string errorMessage, string? stackTrace = null,
            string? errorCode = null, string? errorType = null, string? source = null,
            string? requestMethod = null, string? requestData = null,
            string? targetSite = null, string? additionalInfo = null)
        {
            var log = new ErrorLog
            {
                Timestamp = DateTime.UtcNow,
                LogLevel = "Error",
                LogType = "Error",
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                ErrorCode = errorCode,
                ErrorType = errorType,
                Source = source,
                RequestMethod = requestMethod,
                RequestData = requestData,
                TargetSite = targetSite,
                AdditionalInfo = additionalInfo,
                IsResolved = false
            };

            await EnrichLogWithContextData(log);
            await _context.ErrorLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(DateTime? startDate = null,
            DateTime? endDate = null, string? logType = null, string? userId = null,
            string? action = null, int? page = 1, int? pageSize = 50)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(logType))
                query = query.Where(l => l.LogType == logType);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId.ToString() == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action.Contains(action));

            query = query.OrderByDescending(l => l.Timestamp)
                         .Skip(((page ?? 1) - 1) * (pageSize ?? 50))
                         .Take(pageSize ?? 50);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ErrorLog>> GetErrorLogsAsync(DateTime? startDate = null,
            DateTime? endDate = null, string? errorType = null, bool? isResolved = null,
            int? page = 1, int? pageSize = 50)
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(errorType))
                query = query.Where(l => l.ErrorType == errorType);

            if (isResolved.HasValue)
                query = query.Where(l => l.IsResolved == isResolved.Value);

            query = query.OrderByDescending(l => l.Timestamp)
                         .Skip(((page ?? 1) - 1) * (pageSize ?? 50))
                         .Take(pageSize ?? 50);

            return await query.ToListAsync();
        }

        public async Task<bool> MarkErrorAsResolvedAsync(long errorLogId, string resolution, int resolvedByUserId)
        {
            var errorLog = await _context.ErrorLogs.FindAsync(errorLogId);
            if (errorLog == null) return false;

            errorLog.IsResolved = true;
            errorLog.Resolution = resolution;
            errorLog.ResolvedAt = DateTime.UtcNow;
            errorLog.ResolvedByUserId = resolvedByUserId;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnrichLogWithContextData(BaseLog log)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                log.IpAddress = context.Connection.RemoteIpAddress?.ToString();
                log.RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                log.UserAgent = context.Request.Headers["User-Agent"].ToString();

                // Parse user agent for device info
                ParseUserAgent(log);

                // Get user ID if authenticated
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = context.User.FindFirst("sub");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        log.UserId = userId;
                    }
                }
            }
        }

        private void ParseUserAgent(BaseLog log)
        {
            if (string.IsNullOrEmpty(log.UserAgent)) return;

            // Basic device type detection
            if (Regex.IsMatch(log.UserAgent, "Mobile|iP(hone|od|ad)|Android|BlackBerry|IEMobile", RegexOptions.IgnoreCase))
                log.DeviceType = "Mobile";
            else if (Regex.IsMatch(log.UserAgent, "Tablet|iPad", RegexOptions.IgnoreCase))
                log.DeviceType = "Tablet";
            else
                log.DeviceType = "Desktop";

            // Basic browser detection
            if (log.UserAgent.Contains("Chrome"))
                log.BrowserType = "Chrome";
            else if (log.UserAgent.Contains("Firefox"))
                log.BrowserType = "Firefox";
            else if (log.UserAgent.Contains("Safari"))
                log.BrowserType = "Safari";
            else if (log.UserAgent.Contains("Edge"))
                log.BrowserType = "Edge";
            else if (log.UserAgent.Contains("MSIE") || log.UserAgent.Contains("Trident/"))
                log.BrowserType = "Internet Explorer";

            // Basic OS detection
            if (log.UserAgent.Contains("Windows"))
                log.OperatingSystem = "Windows";
            else if (log.UserAgent.Contains("Mac OS"))
                log.OperatingSystem = "MacOS";
            else if (log.UserAgent.Contains("Linux"))
                log.OperatingSystem = "Linux";
            else if (log.UserAgent.Contains("Android"))
                log.OperatingSystem = "Android";
            else if (log.UserAgent.Contains("iOS"))
                log.OperatingSystem = "iOS";
        }
    }
}