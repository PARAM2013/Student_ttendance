using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course Name is required")]
        [StringLength(100, ErrorMessage = "Name can not be greater then 100 charactors.")]
        public string Name { get; set; }
    }
}