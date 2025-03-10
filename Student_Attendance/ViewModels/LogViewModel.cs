using Student_Attendance.Models.Logging;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class LogViewModel
    {
        // Filter properties
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? LogType { get; set; } // "Activity" or "Error"
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? Module { get; set; }
        public string? UserId { get; set; }
        public string? ErrorType { get; set; }
        public bool? IsResolved { get; set; }
        
        // Pagination properties
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        
        // Results
        public IEnumerable<ActivityLog>? ActivityLogs { get; set; }
        public IEnumerable<ErrorLog>? ErrorLogs { get; set; }
    }
    
    public class ErrorResolutionViewModel
    {
        public long ErrorLogId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Resolution { get; set; }
    }
    
    public class LogDetailsViewModel
    {
        public ActivityLog? ActivityLog { get; set; }
        public ErrorLog? ErrorLog { get; set; }
        public string LogType { get; set; } // "Activity" or "Error"
    }
    
    public class LogStatisticsViewModel
    {
        public int TotalActivityLogs { get; set; }
        public int TotalErrorLogs { get; set; }
        public int UnresolvedErrors { get; set; }
        public int SuccessfulActivities { get; set; }
        public int FailedActivities { get; set; }
        public Dictionary<string, int> ActivityByModule { get; set; }
        public Dictionary<string, int> ErrorsByType { get; set; }
    }
}