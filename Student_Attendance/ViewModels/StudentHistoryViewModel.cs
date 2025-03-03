namespace Student_Attendance.ViewModels
{
    public class StudentHistoryViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string EnrollmentNo { get; set; }
        public List<EnrollmentHistoryRecord> EnrollmentHistory { get; set; } = new();
        public List<AttendanceHistoryRecord> AttendanceHistory { get; set; } = new();
    }

    public class EnrollmentHistoryRecord
    {
        public string StudentName { get; set; }
        public string EnrollmentNo { get; set; }
        public string AcademicYear { get; set; }
        public string Course { get; set; }
        public string Class { get; set; }
        public string Division { get; set; }
        public int Semester { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class AttendanceHistoryRecord
    {
        public string AcademicYear { get; set; }
        public string SubjectName { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public decimal Percentage { get; set; }
    }
}
