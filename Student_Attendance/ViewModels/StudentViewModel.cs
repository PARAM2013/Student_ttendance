using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enrollment No is required")]
        [StringLength(20, ErrorMessage = "Enrollment No can not be greater then 20 charactors.")]
        public string EnrollmentNo { get; set; }

        [Required(ErrorMessage = "Student Name is required")]
        [StringLength(100, ErrorMessage = "Name can not be greater then 100 charactors.")]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Cast { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? Mobile { get; set; }
        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }
        [Required(ErrorMessage = "Semester is required")]
        public int Semester { get; set; }

        public bool IsActive { get; set; } = true;
        [Required(ErrorMessage = "Academic Year is required")]
        public int AcademicYearId { get; set; }
        [Required(ErrorMessage = "Division is required.")]
        public int DivisionId { get; set; }
        public List<SelectListItem> Courses { get; set; }
        public List<SelectListItem> AcademicYears { get; set; }
        public List<SelectListItem> Divisions { get; set; }
    }
}