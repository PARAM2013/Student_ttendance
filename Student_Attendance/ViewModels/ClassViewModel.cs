using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class ClassViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Academic Year is required")]
        public int AcademicYearId { get; set; }

        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AcademicYears { get; set; } = new List<SelectListItem>();
    }


}