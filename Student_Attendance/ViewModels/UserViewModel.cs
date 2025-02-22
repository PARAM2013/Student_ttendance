using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }  // Made nullable and removed Required attribute

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        public List<string> Roles { get; set; } = new List<string>();

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;  // Default to true
    }
}