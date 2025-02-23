namespace Student_Attendance.ViewModels
{
    public class BulkAttendanceSaveModel
    {
        public int SubjectId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public Dictionary<string, Dictionary<string, bool?>> AttendanceData { get; set; } = new();
    }
}
