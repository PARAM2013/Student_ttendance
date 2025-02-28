namespace Student_Attendance.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            Logo = "/images/default-logo.png";
            ShortName = "SA";
            Name = "Student Attendance System";
            CourseAttendance = new List<CourseAttendance>();
            DebugInfo = new DebugInfo();
        }

        public string Logo { get; set; }
        public string ShortName { get; set; }
        public string Name { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public bool HasAttendanceData { get; set; }
        public List<CourseAttendance> CourseAttendance { get; set; }
        public DebugInfo DebugInfo { get; set; }
    }

    public class CourseAttendance
    {
        public CourseAttendance()
        {
            Subjects = new List<SubjectAttendanceData>();
        }

        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public List<SubjectAttendanceData> Subjects { get; set; }
    }

    public class SubjectAttendanceData
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
    }

    public class DebugInfo
    {
        public int TotalRecords { get; set; }
        public int SubjectsWithAttendance { get; set; }
        public int CoursesWithAttendance { get; set; }
    }
}