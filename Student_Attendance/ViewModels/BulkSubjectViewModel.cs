namespace Student_Attendance.ViewModels
{
    public class BulkSubjectViewModel
    {
        public List<BulkSubjectEntry> Subjects { get; set; }
        public int CourseId { get; set; }
        public int ClassId { get; set; }
        public int Semester { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class BulkSubjectEntry
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
