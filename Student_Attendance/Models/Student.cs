using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string SSID { get; set; }

        [Required]
        [StringLength(20)]
        public string EnrollmentNo { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Cast { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? Mobile { get; set; }

        [Required]
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        [Required]
        public int Semester { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public int AcademicYearId { get; set; }
        [ForeignKey("AcademicYearId")]
        public AcademicYear AcademicYear { get; set; }

        [Required]
        public int DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public Division Division { get; set; }       

        public int ClassId { get; set; }
        public Class Class { get; set; }

        public virtual ICollection<StudentSubject> StudentSubjects { get; set; }
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; }

        public Student()
        {
            StudentSubjects = new HashSet<StudentSubject>();
            AttendanceRecords = new HashSet<AttendanceRecord>();
        }
    }
}