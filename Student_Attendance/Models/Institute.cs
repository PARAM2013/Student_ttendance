using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.Models
{
    public class Institute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string ShortName { get; set; }

        [StringLength(500)]
        public string? Logo { get; set; } = "/Images/logos/Defult_logo.jpg";  // Set default logo

        [Required]
        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string ContactNo { get; set; }
    }
}