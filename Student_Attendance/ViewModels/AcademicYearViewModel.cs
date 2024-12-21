using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class AcademicYearViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Academic Year name is required")]
        [StringLength(20, ErrorMessage = "Name can not be greater then 20 charactors.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Start Date is required.")]
        public DateTime StartDate { get; set; }
        [Required(ErrorMessage = "End Date is required.")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

    }
}