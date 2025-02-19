using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class TeacherSubjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a teacher")]
        [Display(Name = "Teacher")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please select a subject")]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Please select an academic year")]
        [Display(Name = "Academic Year")]
        public int AcademicYearId { get; set; }

        public List<SelectListItem> Teachers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Subjects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AcademicYears { get; set; } = new List<SelectListItem>();
    }
}
