using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Student_Attendance.ViewModels
{
    public class LessAttendanceReportViewModel
    {
        public int? CourseId { get; set; }
        public int? ClassId { get; set; }
        public int? DivisionId { get; set; }
        public int? SubjectId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int ThresholdPercentage { get; set; } = 75; // Default threshold

        public SelectList Courses { get; set; }
        public SelectList Classes { get; set; }
        public SelectList Divisions { get; set; }
        public SelectList Subjects { get; set; }
        
        public List<LessAttendanceStudentViewModel> Students { get; set; } = new List<LessAttendanceStudentViewModel>();
    }

    public class LessAttendanceStudentViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public string StudentName { get; set; }
        public string Division { get; set; }
        public string Class { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string RiskLevel { get; set; }
        public bool IsDecreasing { get; set; }
        public int ConsecutiveAbsences { get; set; }
        public Dictionary<string, decimal> SubjectWiseAttendance { get; set; } = new Dictionary<string, decimal>();
    }
}