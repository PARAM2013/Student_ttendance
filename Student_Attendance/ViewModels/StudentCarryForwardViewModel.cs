using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;

namespace Student_Attendance.ViewModels
{
    public class StudentCarryForwardViewModel
    {
        [JsonPropertyName("currentAcademicYearId")]
        public int CurrentAcademicYearId { get; set; }

        [JsonPropertyName("nextAcademicYearId")]
        public int NextAcademicYearId { get; set; }

        [JsonPropertyName("courseId")]
        public int CourseId { get; set; }

        public int? ClassId { get; set; }
        
        public int? DivisionId { get; set; }

        [JsonPropertyName("nextCourseId")]
        public int NextCourseId { get; set; }

        [JsonPropertyName("nextClassId")]
        public int? NextClassId { get; set; }

        [JsonPropertyName("nextDivisionId")]
        public int? NextDivisionId { get; set; }

        public SelectList AcademicYears { get; set; }
        public SelectList NextAcademicYears { get; set; }
        public SelectList Courses { get; set; }
        public SelectList Classes { get; set; }
        public SelectList Divisions { get; set; }
        public SelectList NextCourses { get; set; }
        public SelectList NextClasses { get; set; }
        public SelectList NextDivisions { get; set; }

        public bool DataMoved { get; set; }

        [JsonPropertyName("studentsToPromote")]
        public List<StudentPromotionData> StudentsToPromote { get; set; } = new();
    }

    public class StudentPromotionData
    {
        [JsonPropertyName("studentId")]
        public int StudentId { get; set; }

        [JsonPropertyName("enrollmentNo")]
        public string EnrollmentNo { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("currentSemester")]
        public int CurrentSemester { get; set; }

        [JsonPropertyName("nextSemester")]
        public int NextSemester { get; set; }

        [JsonPropertyName("currentClass")]
        public string CurrentClass { get; set; }

        [JsonPropertyName("nextClass")]
        public string NextClass { get; set; }

        [JsonPropertyName("selected")]
        public bool Selected { get; set; }

        public string CurrentDivision { get; set; }
        public string NextDivision { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
