using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        public int? SpecializationId { get; set; }
        [ForeignKey("SpecializationId")]
        public Specialization? Specialization { get; set; }

        [Required]
        public int AcademicYearId { get; set; }
        [ForeignKey("AcademicYearId")]
        public AcademicYear AcademicYear { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Student> Students { get; set; }
    }
}