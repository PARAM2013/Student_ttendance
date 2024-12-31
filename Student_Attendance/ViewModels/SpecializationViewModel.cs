using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class SpecializationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Specialization Name is required")]
        [StringLength(100, ErrorMessage = "Name can not be greater then 100 charactors.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        public int CourseId { get; set; }

        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
    }
}
