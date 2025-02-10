using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class AttendanceViewModel
    {
        public int SubjectId { get; set; }
        public DateTime Date { get; set; }
        public List<StudentAttendanceViewModel> Students { get; set; }
        public SelectList Subjects { get; set; }
        public SelectList Divisions { get; set; }
    }
}
