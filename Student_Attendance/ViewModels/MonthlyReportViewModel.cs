using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class MonthlyReportViewModel
    {
        public DateTime ReportDate { get; set; }
        public int? TeacherId { get; set; }
        public SelectList? Teachers { get; set; }
        public SelectList? Subjects { get; set; }
        public bool SkipEmptyDates { get; set; } = true;
        public string? TeacherFilter { get; set; }
    }

    public class StudentMonthlyAttendance
    {
        public string EnrollmentNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public Dictionary<DateTime, bool?> AttendanceByDate { get; set; } = new();
    }
}
