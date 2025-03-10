using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.Models.Logging
{
    public class ActivityLog : BaseLog
    {
        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; }

        public string? EntityId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(200)]
        public string? OldValue { get; set; }

        [StringLength(200)]
        public string? NewValue { get; set; }

        [StringLength(100)]
        public string? Module { get; set; }

        [StringLength(500)]
        public new string? RequestUrl { get; set; }

        public bool IsSuccess { get; set; }

        [StringLength(100)]
        public string? Status { get; set; }
    }
}