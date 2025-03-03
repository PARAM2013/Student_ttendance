using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class SubjectMappingViewModel
    {
        public IFormFile? File { get; set; }
        public int? ClassId { get; set; }
        public int? DivisionId { get; set; }
        public List<SelectListItem> Classes { get; set; } = new();
        public List<SelectListItem> Divisions { get; set; } = new();
        public List<string> ImportErrors { get; set; } = new(); // Add this property
    }

    public class SubjectMappingPreviewModel
    {
        public List<SubjectMappingRow> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public int UpdatedMappings { get; set; }
        public int NewMappings { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? FileId { get; set; }
    }

    public class SubjectMappingRow
    {
        public int RowNumber { get; set; }
        public string? EnrollmentNo { get; set; }
        public string? StudentName { get; set; }
        public string? Class { get; set; }
        public string? Division { get; set; }
        public List<string> SubjectCodes { get; set; } = new();
        public string? StatusMessage { get; set; }
        public MappingStatus Status { get; set; }
    }

    public enum MappingStatus
    {
        Valid,
        Invalid,
        NoChange
    }
}
