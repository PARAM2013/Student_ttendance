using Student_Attendance.Models.Logging;

namespace Student_Attendance.Services.Logging
{
    public interface ILoggingService
    {
        Task LogActivityAsync(string action, string entityType, string? entityId = null, string? details = null,
            string? oldValue = null, string? newValue = null, string? module = null, bool isSuccess = true,
            string? status = null);

        Task LogErrorAsync(string errorMessage, string? stackTrace = null, string? errorCode = null,
            string? errorType = null, string? source = null, string? requestMethod = null,
            string? requestData = null, string? targetSite = null, string? additionalInfo = null);

        Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(DateTime? startDate = null, DateTime? endDate = null,
            string? logType = null, string? userId = null, string? action = null, int? page = 1, int? pageSize = 50);

        Task<IEnumerable<ErrorLog>> GetErrorLogsAsync(DateTime? startDate = null, DateTime? endDate = null,
            string? errorType = null, bool? isResolved = null, int? page = 1, int? pageSize = 50);

        Task<bool> MarkErrorAsResolvedAsync(long errorLogId, string resolution, int resolvedByUserId);
    }
}