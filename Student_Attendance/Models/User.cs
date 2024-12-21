using System.ComponentModel.DataAnnotations;
namespace Student_Attendance.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }
        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }
        [Required]
        [StringLength(20)]
        public string Role { get; set; }
    }
}