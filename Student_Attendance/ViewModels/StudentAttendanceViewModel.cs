namespace Student_Attendance.ViewModels
{
    public class StudentAttendanceViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public string StudentName { get; set; }
        public bool IsPresent { get; set; }
        public string AbsenceReason { get; set; }
    }
}
