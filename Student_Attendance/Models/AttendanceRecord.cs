using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public bool IsPresent { get; set; }

        public DateTime TimeStamp { get; set; }

        [Required]
        public int MarkedById { get; set; }
        [ForeignKey("MarkedById")]
        public User MarkedBy { get; set; }

        [StringLength(500)]
        public string? DiscussionTopic { get; set; }
    }
}