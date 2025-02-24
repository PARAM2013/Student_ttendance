using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Student_Attendance.ViewModels
{
    public class StudentImportViewModel
    {
        public IFormFile? File { get; set; }
        public List<string> ImportErrors { get; set; } = new List<string>();
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int NewStudentsCount { get; set; }
        public int UpdatedStudentsCount { get; set; }
    }
}