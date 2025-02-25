namespace Student_Attendance.ViewModels
{
    public class ImportPreviewViewModel
    {
        public List<StudentImportRow> Students { get; set; } = new();
        public int TotalRows { get; set; }
        public int NewStudents { get; set; }
        public int UpdatedStudents { get; set; }
        public int DuplicatesInFile { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? FileId { get; set; }
    }

    public class StudentImportRow
    {
        public string? EnrollmentNo { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Cast { get; set; }
        public string? Course { get; set; }
        public string? Class { get; set; }
        public string? Division { get; set; }
        public int? Semester { get; set; }
        public string? AcademicYear { get; set; }
        public ImportRowStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public int RowNumber { get; set; }
    }

    public enum ImportRowStatus
    {
        New,
        Update,
        Error,
        Duplicate
    }
}
