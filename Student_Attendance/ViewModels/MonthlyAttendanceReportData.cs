namespace Student_Attendance.ViewModels
{
    public class MonthlyAttendanceReportData
    {
        public string InstituteName { get; set; } = string.Empty;
        public string InstituteAddress { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectInfo { get; set; } = string.Empty;
        public string ClassInfo { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string MonthYear { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public List<DateTime> Dates { get; set; } = new();
        public List<StudentMonthlyAttendance> Students { get; set; } = new();
        public List<string> TeacherAttribution { get; set; } = new();
    }
}
