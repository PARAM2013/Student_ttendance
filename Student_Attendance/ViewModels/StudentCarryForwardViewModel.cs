using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class StudentCarryForwardViewModel
    {
        public int CurrentAcademicYearId { get; set; }
        public int NextAcademicYearId { get; set; }
        public int CourseId { get; set; }
        public int? ClassId { get; set; }
        public int? DivisionId { get; set; }

        public SelectList AcademicYears { get; set; }
        public SelectList NextAcademicYears { get; set; }
        public SelectList Courses { get; set; }
        public SelectList Classes { get; set; }
        public SelectList Divisions { get; set; }

        public bool DataMoved { get; set; }
        public List<StudentPromotionData> StudentsToPromote { get; set; } = new();
    }

    public class StudentPromotionData
    {
        public int StudentId { get; set; }
        public string EnrollmentNo { get; set; }
        public string Name { get; set; }
        public int CurrentSemester { get; set; }
        public int NextSemester { get; set; }
        public string CurrentClass { get; set; }
        public string NextClass { get; set; }
        public bool Selected { get; set; } = true;
    }
}
