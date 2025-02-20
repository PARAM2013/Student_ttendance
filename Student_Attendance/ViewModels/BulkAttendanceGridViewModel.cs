using System;
using System.Collections.Generic;

namespace Student_Attendance.ViewModels
{
    public class BulkAttendanceGridViewModel
    {
        public List<DateTime> Dates { get; set; } = new List<DateTime>();
        public List<BulkAttendanceStudentViewModel> Students { get; set; } = new List<BulkAttendanceStudentViewModel>();
        public Dictionary<(int, DateTime), bool> ExistingAttendance { get; set; } = new Dictionary<(int, DateTime), bool>();
    }
}
