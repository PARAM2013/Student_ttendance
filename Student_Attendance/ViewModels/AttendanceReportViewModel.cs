using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Student_Attendance.ViewModels
{
    public class AttendanceReportViewModel
    {
        public int SubjectId { get; set; }
        public int DivisionId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AttendanceReportItemViewModel> AttendanceData { get; set; }
        public SelectList Subjects { get; set; }
        public SelectList Divisions { get; set; }
    }

    public class AttendanceReportItemViewModel
    {
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public string StudentName { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}