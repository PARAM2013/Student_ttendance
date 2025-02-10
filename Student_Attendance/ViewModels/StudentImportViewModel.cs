using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Student_Attendance.ViewModels
{
    public class StudentImportViewModel
    {
        public IFormFile File { get; set; }
        public List<string> ImportErrors { get; set; } = new List<string>();
    }
}