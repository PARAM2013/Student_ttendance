using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Student_Attendance.Controllers
{
    public class InstituteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InstituteController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string DEFAULT_LOGO = "/Images/logos/Defult_logo.jpg";

        public InstituteController(ApplicationDbContext context,
            ILogger<InstituteController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var institute = await _context.Institutes.FirstOrDefaultAsync();
            return View(institute);
        }

        public IActionResult Create()
        {
            if (_context.Institutes.Any())
            {
                return RedirectToAction(nameof(Index));
            }
            return View(new InstituteViewModel { Logo = DEFAULT_LOGO });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InstituteViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var institute = new Institute
                    {
                        Name = model.Name.Trim(),
                        ShortName = model.ShortName.Trim(),
                        Logo = DEFAULT_LOGO,
                        Address = model.Address.Trim(),
                        Website = model.Website?.Trim(),
                        Email = model.Email.Trim(),
                        ContactNo = model.ContactNo.Trim()
                    };

                    if (model.LogoFile != null)
                    {
                        institute.Logo = await SaveLogoFile(model.LogoFile, model.ShortName);
                    }

                    _context.Add(institute);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created new institute: {institute.Name}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating institute");
                    ModelState.AddModelError("", "Unable to create institute. Please try again.");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
            {
                return NotFound();
            }

            var viewModel = new InstituteViewModel
            {
                Id = institute.Id,
                Name = institute.Name,
                ShortName = institute.ShortName,
                Logo = institute.Logo ?? DEFAULT_LOGO,
                Address = institute.Address,
                Website = institute.Website,
                Email = institute.Email,
                ContactNo = institute.ContactNo
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InstituteViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var institute = await _context.Institutes.FindAsync(id);
                    if (institute == null)
                    {
                        return NotFound();
                    }

                    if (model.LogoFile != null)
                    {
                        // Delete old logo if it's not the default logo
                        if (!string.IsNullOrEmpty(institute.Logo) && institute.Logo != DEFAULT_LOGO)
                        {
                            DeleteOldLogo(institute.Logo);
                        }
                        institute.Logo = await SaveLogoFile(model.LogoFile, model.ShortName);
                    }

                    institute.Name = model.Name.Trim();
                    institute.ShortName = model.ShortName.Trim();
                    institute.Address = model.Address.Trim();
                    institute.Website = model.Website?.Trim();
                    institute.Email = model.Email.Trim();
                    institute.ContactNo = model.ContactNo.Trim();

                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated institute: {institute.Name}");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!InstituteExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating institute");
                        ModelState.AddModelError("", "The record was modified by another user. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating institute");
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            return View(model);
        }

        private bool InstituteExists(int id)
        {
            return _context.Institutes.Any(e => e.Id == id);
        }

        private async Task<string> SaveLogoFile(IFormFile file, string shortName)
        {
            // Validate file type
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only .jpg, .jpeg and .png files are allowed.");
            }

            // Validate file size (max 2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("File size cannot exceed 2MB.");
            }

            // Create filename in format: ShortName_ddMMyyyyss
            string timestamp = DateTime.Now.ToString("ddMMyyyyss");
            string sanitizedShortName = shortName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
            string uniqueFileName = $"{sanitizedShortName}_{timestamp}{fileExtension}";

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "logos");
            Directory.CreateDirectory(uploadsFolder);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/Images/logos/{uniqueFileName}";
        }


        private void DeleteOldLogo(string logoPath)
        {
            if (string.IsNullOrEmpty(logoPath) || logoPath == DEFAULT_LOGO)
            {
                return;
            }

            try
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, logoPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Deleted old logo: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting old logo: {logoPath}");
            }
        }

        
    }
}