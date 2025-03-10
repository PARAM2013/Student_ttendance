using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models.Logging
{
    public abstract class BaseLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(50)]
        public string LogLevel { get; set; }

        [Required]
        [StringLength(50)]
        public string LogType { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(50)]
        public string? DeviceType { get; set; }

        [StringLength(50)]
        public string? BrowserType { get; set; }

        [StringLength(50)]
        public string? OperatingSystem { get; set; }

        [StringLength(50)]
        public string? ScreenResolution { get; set; }

        [StringLength(50)]
        public string? NetworkType { get; set; }

        [StringLength(500)]
        public string? RequestUrl { get; set; }
    }
}