using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.Models
{
    public class StudentEnrollmentHistory
    {
        [Key]
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public int AcademicYearId { get; set; }
        public int CourseId { get; set; }
        public int ClassId { get; set; }
        public int? DivisionId { get; set; }
        public int Semester { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public bool IsActive { get; set; }

        public virtual Student Student { get; set; }
    }
}
