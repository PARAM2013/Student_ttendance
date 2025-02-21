using System;
using System.Collections.Generic;

namespace Student_Attendance.Models
{
    public class BulkAttendanceSaveModel
    {
        public int SubjectId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public Dictionary<string, Dictionary<string, bool>> AttendanceData { get; set; } = new(); // Changed type to string keys
    }
}
