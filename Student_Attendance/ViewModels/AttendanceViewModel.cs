using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class AttendanceViewModel
    {
        public int SubjectId { get; set; }
        public int DivisionId { get; set; }  // Add this property
        public DateTime Date { get; set; }
        public List<StudentAttendanceViewModel> Students { get; set; }
        public SelectList Subjects { get; set; }
        public SelectList Divisions { get; set; }

        [Required(ErrorMessage = "Topic discussed is required")]
        [StringLength(500)]
        public string TopicDiscussed { get; set; }

        public string TeacherName { get; set; }
    }
}
