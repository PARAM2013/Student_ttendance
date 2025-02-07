using System.ComponentModel.DataAnnotations;

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
    public IFormFile? LogoFile { get; set; }

    public string? Logo { get; set; }

    public const string DefaultLogoPath = "/Images/logos/Defult_logo.jpg";

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? Website { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? ContactNo { get; set; }
}