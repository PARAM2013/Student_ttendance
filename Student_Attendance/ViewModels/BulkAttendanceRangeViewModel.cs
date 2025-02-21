using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace Student_Attendance.ViewModels
{
    public class BulkAttendanceRangeViewModel : BulkAttendanceViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
    }
}
