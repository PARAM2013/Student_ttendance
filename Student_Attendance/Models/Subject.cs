using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        [StringLength(20)]
        public string Code { get; set; }
        public int? SpecializationId { get; set; }
        [ForeignKey("SpecializationId")]
        public Specialization? Specialization { get; set; }
        [Required]
        public int Semester { get; set; }

        [Required]
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }
        [Required]
        public int AcademicYearId { get; set; }
        [ForeignKey("AcademicYearId")]
        public AcademicYear AcademicYear { get; set; }

        // Added property for class relationship
        public int ClassId { get; set; }
        // Optionally add navigation property if needed
        public Class Class { get; set; }
    }
}