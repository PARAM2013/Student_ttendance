using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class MonthlyReportViewModel
    {
        public int? TeacherId { get; set; }
        public int? SubjectId { get; set; }
        public DateTime ReportDate { get; set; }
        public bool SkipEmptyDates { get; set; }
        public SelectList? Teachers { get; set; }
        public SelectList? Subjects { get; set; }
    }

    public class MonthlyAttendanceReportData
    {
        public string InstituteName { get; set; }
        public string TeacherName { get; set; }
        public string SubjectInfo { get; set; }
        public string ClassInfo { get; set; }
        public string DivisionName { get; set; }
        public string AcademicYear { get; set; }
        public string MonthYear { get; set; }
        public List<DateTime> Dates { get; set; }
        public List<StudentMonthlyAttendance> Students { get; set; }
        public string? InstituteAddress { get; set; }
        public string? CourseName { get; set; }
        public string? Specialization { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    public class StudentMonthlyAttendance
    {
        public string EnrollmentNo { get; set; }
        public string StudentName { get; set; }
        public Dictionary<DateTime, bool?> AttendanceByDate { get; set; }
    }
}
