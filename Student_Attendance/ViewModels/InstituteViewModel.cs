using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Student_Attendance.ViewModels
{
    public class InstituteViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Institute Name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Short Name is required")]
        [StringLength(50)]
        public string ShortName { get; set; }

        [Display(Name = "Logo")]
        public IFormFile LogoFile { get; set; }  // For file upload

        public string Logo { get; set; }  // For storing file path

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact No is required")]
        [StringLength(20)]
        public string ContactNo { get; set; }
    }
}