﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Student_Attendance.Models;

namespace Student_Attendance.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Student ID")]
        [DisplayFormat(NullDisplayText = "Not Assigned")]
        public string? SSID { get; set; } // Make it nullable and read-only

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
        [Range(1, int.MaxValue, ErrorMessage = "Please select a Course")]
        public int? CourseId { get; set; }

        [Required(ErrorMessage = "Semester is Required")]
        [Range(1, 12, ErrorMessage = "Semester must be between 1 and 12")]
        public int? Semester { get; set; }

        [Required(ErrorMessage = "Class is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a Class")]
        public int? ClassId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Academic Year is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an Academic Year")]
        public int? AcademicYearId { get; set; }

        [Required(ErrorMessage = "Division is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a Division")]
        public int? DivisionId { get; set; }

        // Navigation properties
        public Course? Course { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public Division? Division { get; set; }
        public Class? Class { get; set; }

        // Dropdowns
        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AcademicYears { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Divisions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Classes { get; set; } = new List<SelectListItem>();
    }
}