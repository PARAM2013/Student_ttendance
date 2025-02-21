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

        [StringLength(100)]
        public string? Password { get; set; }  // Optional for editing

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
}