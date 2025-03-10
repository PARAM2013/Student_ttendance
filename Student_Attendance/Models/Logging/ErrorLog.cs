using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.Models.Logging
{
    public class ErrorLog : BaseLog
    {
        [Required]
        [StringLength(200)]
        public string ErrorMessage { get; set; }

        [StringLength(4000)]
        public string? StackTrace { get; set; }

        [StringLength(100)]
        public string? ErrorCode { get; set; }

        [StringLength(100)]
        public string? ErrorType { get; set; }

        [StringLength(500)]
        public string? Source { get; set; }

        [StringLength(500)]
        public new string? RequestUrl { get; set; }

        [StringLength(100)]
        public string? RequestMethod { get; set; }

        [StringLength(4000)]
        public string? RequestData { get; set; }

        [StringLength(100)]
        public string? TargetSite { get; set; }

        [StringLength(500)]
        public string? AdditionalInfo { get; set; }

        public bool IsResolved { get; set; }

        [StringLength(100)]
        public string? Resolution { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }
    }
}