using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Attendance.Models
{
    public class StudentAttendanceArchive
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public string EnrollmentNo { get; set; }

        [Required]
        public string StudentName { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public bool IsPresent { get; set; }

        [Required]
        public int AcademicYearId { get; set; }

        [Required]
        public int MarkedById { get; set; }

        public DateTime ArchivedOn { get; set; }
    }
}
