using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;

namespace Student_Attendance.Controllers
{
    public class InstituteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InstituteController> _logger;

        public InstituteController(ApplicationDbContext context, ILogger<InstituteController> logger)
        {
            _context = context;
            _logger = logger;
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
                    var institute = new Institute
                    {
                        Name = model.Name,
                        ShortName = model.ShortName,
                        Logo = model.Logo,
                        Address = model.Address,
                        Website = model.Website,
                        Email = model.Email,
                        ContactNo = model.ContactNo
                    };

                    _context.Add(institute);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating institute");
                    ModelState.AddModelError("", "Unable to save changes. Try again.");
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

                    institute.Name = model.Name;
                    institute.ShortName = model.ShortName;
                    institute.Logo = model.Logo;
                    institute.Address = model.Address;
                    institute.Website = model.Website;
                    institute.Email = model.Email;
                    institute.ContactNo = model.ContactNo;

                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstituteExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        private bool InstituteExists(int id)
        {
            return _context.Institutes.Any(e => e.Id == id);
        }
    }
}