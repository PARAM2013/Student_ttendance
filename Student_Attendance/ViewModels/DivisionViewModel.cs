using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class DivisionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Division Name is required")]
        [StringLength(100, ErrorMessage = "Name can not be greater then 100 charactors.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Class is required.")]
        public int ClassId { get; set; }

        public List<SelectListItem> Classes { get; set; } = new List<SelectListItem>();  // Initialize the list
    }

}