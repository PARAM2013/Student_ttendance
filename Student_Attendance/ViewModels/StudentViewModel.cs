using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Student_Attendance.Models;

namespace Student_Attendance.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enrollment Number is Required")]
        [StringLength(20)]
        public string EnrollmentNo { get; set; }

        [Required(ErrorMessage = "Name is Required")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Cast { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? Mobile { get; set; }

        [Required(ErrorMessage = "Course is Required")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Semester is Required")]
        public int Semester { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Academic Year is Required")]
        public int AcademicYearId { get; set; }

        [Required(ErrorMessage = "Division is Required")]
        public int DivisionId { get; set; }

        // Dropdowns
        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AcademicYears { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Divisions { get; set; } = new List<SelectListItem>();

        public Course Course { get; set; }
        public AcademicYear AcademicYear { get; set; }
        public Division Division { get; set; }
    }
}
    
