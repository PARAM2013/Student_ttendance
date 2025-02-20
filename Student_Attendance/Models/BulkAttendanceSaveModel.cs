using System;
using System.Collections.Generic;

namespace Student_Attendance.Models
{
    public class BulkAttendanceSaveModel
    {
        public int SubjectId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public Dictionary<int, Dictionary<DateTime, bool>> AttendanceData { get; set; } = new Dictionary<int, Dictionary<DateTime, bool>>();
    }
}
