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

        [Required]
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        public int? ClassId { get; set; }
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public int? SpecializationId { get; set; }
        [ForeignKey("SpecializationId")]
        public Specialization? Specialization { get; set; }

        public int Semester { get; set; }

        // Navigation properties
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; }
        public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; }
        public virtual ICollection<StudentSubject> StudentSubjects { get; set; }

        public Subject()
        {
            AttendanceRecords = new HashSet<AttendanceRecord>();
            TeacherSubjects = new HashSet<TeacherSubject>();
            StudentSubjects = new HashSet<StudentSubject>();
        }
    }
}