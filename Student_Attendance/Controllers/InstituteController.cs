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

        public InstituteController(ApplicationDbContext context, 
            ILogger<InstituteController> logger, 
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Institute
        public async Task<IActionResult> Index()
        {
            var institute = await _context.Institutes.FirstOrDefaultAsync();
            return View(institute);
        }

        // GET: Institute/Create
        public IActionResult Create()
        {
            // Check if institute already exists
            if (_context.Institutes.Any())
            {
                return RedirectToAction(nameof(Index));
            }
            return View(new InstituteViewModel());
        }

        // POST: Institute/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InstituteViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string logoPath = InstituteViewModel.DefaultLogoPath;

                    if (model.LogoFile != null)
                    {
                        // Validate file type
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                        string fileExtension = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("LogoFile", "Only .jpg, .jpeg and .png files are allowed");
                            return View(model);
                        }

                        // Validate file size (max 2MB)
                        if (model.LogoFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("LogoFile", "File size cannot exceed 2MB");
                            return View(model);
                        }

                        // Create directory if it doesn't exist
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "logos");
                        Directory.CreateDirectory(uploadsFolder);

                        // Generate unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.LogoFile.CopyToAsync(fileStream);
                        }

                        logoPath = "/Images/logos/" + uniqueFileName;
                    }

                    var institute = new Institute
                    {
                        Name = model.Name.Trim(),
                        ShortName = model.ShortName.Trim(),
                        Logo = logoPath,  // Use either uploaded image path or default
                        Address = model.Address.Trim(),
                        Website = model.Website?.Trim(),
                        Email = model.Email.Trim(),
                        ContactNo = model.ContactNo.Trim()
                    };

                    _context.Add(institute);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating institute");
                    ModelState.AddModelError("", "Unable to save changes.");
                }
            }
            return View(model);
        }

        // GET: Institute/Edit/5
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
                Logo = institute.Logo,
                Address = institute.Address,
                Website = institute.Website,
                Email = institute.Email,
                ContactNo = institute.ContactNo
            };

            return View(viewModel);
        }

        // POST: Institute/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InstituteViewModel model)
        {
            // Add logging to track the flow
            _logger.LogInformation($"Edit action called for institute {id}");
            _logger.LogInformation($"Logo file submitted: {model.LogoFile?.FileName ?? "no file"}");

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
                        // Log file details
                        _logger.LogInformation($"File size: {model.LogoFile.Length} bytes");
                        _logger.LogInformation($"File type: {model.LogoFile.ContentType}");

                        // Validate file type
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                        string fileExtension = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("LogoFile", "Only .jpg, .jpeg and .png files are allowed");
                            return View(model);
                        }

                        // Validate file size (max 2MB)
                        if (model.LogoFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("LogoFile", "File size cannot exceed 2MB");
                            return View(model);
                        }

                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(institute.Logo))
                        {
                            var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, institute.Logo.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Save new logo
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logos");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        try
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.LogoFile.CopyToAsync(fileStream);
                            }
                            institute.Logo = "/images/logos/" + uniqueFileName;
                            _logger.LogInformation($"Logo saved successfully: {institute.Logo}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error saving file: {ex.Message}");
                            ModelState.AddModelError("LogoFile", "Error saving file");
                            return View(model);
                        }
                    }

                    institute.Name = model.Name.Trim();
                    institute.ShortName = model.ShortName.Trim();
                    institute.Address = model.Address.Trim();
                    institute.Website = model.Website?.Trim();
                    institute.Email = model.Email.Trim();
                    institute.ContactNo = model.ContactNo.Trim();

                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating institute: {ex.Message}");
                    ModelState.AddModelError("", "Unable to save changes.");
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TestUpload(IFormFile file)
        {
            if (file != null)
            {
                _logger.LogInformation($"Received file: {file.FileName}, Size: {file.Length}");
                return Ok(new { fileName = file.FileName, size = file.Length });
            }
            return BadRequest("No file received");
        }

        private bool InstituteExists(int id)
        {
            return _context.Institutes.Any(e => e.Id == id);
        }
    }
}