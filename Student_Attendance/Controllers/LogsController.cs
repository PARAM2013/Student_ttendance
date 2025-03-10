using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models.Logging;
using Student_Attendance.Services.Logging;
using Student_Attendance.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Student_Attendance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : BaseController
    {
        private readonly ILoggingService _loggingService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ApplicationDbContext context, ILoggingService loggingService, ILogger<LogsController> logger)
            : base(context)
        {
            _loggingService = loggingService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(LogViewModel model)
        {
            if (model == null)
            {
                model = new LogViewModel
                {
                    StartDate = DateTime.Today.AddDays(-7),
                    EndDate = DateTime.Today,
                    LogType = "Activity",
                    Page = 1,
                    PageSize = 50
                };
            }

            // Set default dates if not provided
            model.StartDate ??= DateTime.Today.AddDays(-7);
            model.EndDate ??= DateTime.Today;

            // Default to Activity logs if not specified
            model.LogType ??= "Activity";

            if (model.LogType == "Activity")
            {
                var activityLogs = await _loggingService.GetActivityLogsAsync(
                    model.StartDate, model.EndDate, model.LogType, model.UserId, model.Action, model.Page, model.PageSize);
                model.ActivityLogs = activityLogs.Select(log => new ViewModels.ActivityLog(log)).ToList();
                model.TotalItems = await _context.ActivityLogs
                    .Where(l => (!model.StartDate.HasValue || l.Timestamp >= model.StartDate) &&
                           (!model.EndDate.HasValue || l.Timestamp <= model.EndDate) &&
                           (string.IsNullOrEmpty(model.Action) || l.Action == model.Action) &&
                           (string.IsNullOrEmpty(model.EntityType) || l.EntityType == model.EntityType) &&
                           (string.IsNullOrEmpty(model.Module) || l.Module == model.Module) &&
                           (string.IsNullOrEmpty(model.UserId) || l.UserId.ToString() == model.UserId))
                    .CountAsync();
            }
            else if (model.LogType == "Error")
            {
                var errorLogs = await _loggingService.GetErrorLogsAsync(
                    model.StartDate, model.EndDate, model.ErrorType, model.IsResolved, model.Page, model.PageSize);
                model.ErrorLogs = errorLogs;
                model.TotalItems = await _context.ErrorLogs
                    .Where(l => (!model.StartDate.HasValue || l.Timestamp >= model.StartDate) &&
                           (!model.EndDate.HasValue || l.Timestamp <= model.EndDate) &&
                           (string.IsNullOrEmpty(model.ErrorType) || l.ErrorType == model.ErrorType) &&
                           (!model.IsResolved.HasValue || l.IsResolved == model.IsResolved))
                    .CountAsync();
            }

            return View(model);
        }

        public async Task<IActionResult> ActivityLogs(LogViewModel model)
        {
            model.LogType = "Activity";
            return await Index(model);
        }

        public async Task<IActionResult> ErrorLogs(LogViewModel model)
        {
            model.LogType = "Error";
            return await Index(model);
        }

        public async Task<IActionResult> LogDetails(long id, string logType)
        {
            var model = new LogDetailsViewModel
            {
                LogType = logType
            };

            if (logType == "Activity")
            {
                var activityLog = await _context.ActivityLogs.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
                if (activityLog == null)
                {
                    return NotFound();
                }
                model.ActivityLog = new ViewModels.ActivityLog(activityLog);
            }
            else if (logType == "Error")
            {
                model.ErrorLog = await _context.ErrorLogs.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
                if (model.ErrorLog == null)
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest("Invalid log type");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveError(ErrorResolutionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _loggingService.MarkErrorAsResolvedAsync(model.ErrorLogId, model.Resolution, userId.Value);
                if (success)
                {
                    return Json(new { success = true, message = "Error marked as resolved" });
                }
                else
                {
                    return Json(new { success = false, message = "Error not found or already resolved" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving error log");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new LogStatisticsViewModel();

            // Get counts
            model.TotalActivityLogs = await _context.ActivityLogs.CountAsync();
            model.TotalErrorLogs = await _context.ErrorLogs.CountAsync();
            model.UnresolvedErrors = await _context.ErrorLogs.CountAsync(e => !e.IsResolved);
            model.SuccessfulActivities = await _context.ActivityLogs.CountAsync(a => a.IsSuccess);
            model.FailedActivities = await _context.ActivityLogs.CountAsync(a => !a.IsSuccess);

            // Get activity by module
            model.ActivityByModule = await _context.ActivityLogs
                .Where(a => a.Module != null)
                .GroupBy(a => a.Module)
                .Select(g => new { Module = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Module, x => x.Count);

            // Get errors by type
            model.ErrorsByType = await _context.ErrorLogs
                .Where(e => e.ErrorType != null)
                .GroupBy(e => e.ErrorType)
                .Select(g => new { ErrorType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ErrorType, x => x.Count);

            return View(model);
        }

        private int? GetCurrentUserId()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }
    }
}