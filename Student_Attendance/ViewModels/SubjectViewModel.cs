using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class SubjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject Name is required")]
        [StringLength(100, ErrorMessage = "Name can not be greater then 100 charactors.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Subject Code is required")]
        [StringLength(20, ErrorMessage = "Code can not be greater then 20 charactors.")]
        public string Code { get; set; }

        public int? SpecializationId { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
        public int Semester { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Academic Year is required.")]
        public int AcademicYearId { get; set; }

        public int ClassId { get; set; }
        public List<SelectListItem> Classes { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> Specializations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AcademicYears { get; set; } = new List<SelectListItem>();
    }
}