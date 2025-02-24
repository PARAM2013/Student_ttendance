using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class ClassWiseReportViewModel
    {
        public int? AcademicYearId { get; set; }
        public int? CourseId { get; set; }
        public int? ClassId { get; set; }
        public SelectList? AcademicYears { get; set; }
        public SelectList? Courses { get; set; }
        public SelectList? Classes { get; set; }
    }

    public class ClassWiseReportData
    {
        public string InstituteName { get; set; }
        public string InstituteAddress { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }
        public string AcademicYear { get; set; }
        public string WebsiteUrl { get; set; }
        public List<StudentAttendanceSummary> Students { get; set; }
    }

    public class StudentAttendanceSummary
    {
        public string EnrollmentNo { get; set; }
        public string StudentName { get; set; }
        public string Division { get; set; }
        public string Specialization { get; set; }
        public List<SubjectAttendance> SubjectAttendances { get; set; }
        public decimal OverallPercentage { get; set; }
    }

    public class SubjectAttendance
    {
        public string SubjectName { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public decimal Percentage { get; set; }
    }
}
