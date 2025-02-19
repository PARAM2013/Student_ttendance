using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class TeacherSubject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }

        [Required]
        public int AcademicYearId { get; set; }
        
        [ForeignKey("AcademicYearId")]
        public AcademicYear AcademicYear { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}