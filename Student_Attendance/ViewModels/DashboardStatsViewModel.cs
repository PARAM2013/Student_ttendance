namespace Student_Attendance.ViewModels
{
    public class DashboardStatsViewModel : DashboardViewModel
    {
        public DashboardStatsViewModel()
        {
            RecentActivities = new List<ActivityLog>();
            LowAttendanceStudents = new List<StudentAttendanceAlert>();
            WeeklyTrends = new WeeklyAttendanceData();
        }

        public List<ActivityLog> RecentActivities { get; set; }
        public List<StudentAttendanceAlert> LowAttendanceStudents { get; set; }
        public WeeklyAttendanceData WeeklyTrends { get; set; }
        public double OverallAttendancePercentage { get; set; }
    }

    public class ActivityLog
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string UserName { get; set; }
        public string Details { get; set; }
    }

    public class StudentAttendanceAlert
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Course { get; set; }
        public double AttendancePercentage { get; set; }
    }

    public class WeeklyAttendanceData
    {
        public List<string> Dates { get; set; } = new List<string>();
        public List<double> Percentages { get; set; } = new List<double>();
    }
}
