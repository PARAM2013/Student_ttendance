using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class AttendanceAudit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttendanceRecordId { get; set; }
        [ForeignKey("AttendanceRecordId")]
        public AttendanceRecord AttendanceRecord { get; set; }

        [Required]
        public int ModifiedById { get; set; }
        [ForeignKey("ModifiedById")]
        public User ModifiedBy { get; set; }

        [Required]
        public DateTime ModifiedOn { get; set; }

        [Required]
        public bool OldValue { get; set; }

        [Required]
        public bool NewValue { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; } // Already nullable, just confirming
    }
}
