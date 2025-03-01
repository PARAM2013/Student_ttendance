namespace Student_Attendance.ViewModels
{
    public class BulkSubjectViewModel
    {
        public int CourseId { get; set; }
        public int? ClassId { get; set; }
        public int? SpecializationId { get; set; }
        public int Semester { get; set; }  // Changed to non-nullable
        public List<SubjectInputModel> Subjects { get; set; } = new List<SubjectInputModel>();
    }

    public class SubjectInputModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
