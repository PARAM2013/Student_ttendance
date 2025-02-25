using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class TopicDiscussionsReportViewModel
    {
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SelectList? Teachers { get; set; }
        public SelectList? Subjects { get; set; }
        public List<TopicDiscussionItem>? Discussions { get; set; }
    }

    public class TopicDiscussionItem
    {
        public DateTime Date { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int MarkedById { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string DiscussionTopic { get; set; } = string.Empty;
        public int StudentsPresent { get; set; }
        public int TotalStudents { get; set; }
    }
}
