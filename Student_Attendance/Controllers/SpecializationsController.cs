using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentAttendance.Controllers
{
    public class SpecializationsController : BaseController
    {
        public SpecializationsController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Specializations
        public async Task<IActionResult> Index()
        {
            var specializations = await _context.Specializations.Include(c => c.Course).ToListAsync();
            return View(specializations);
        }


        // POST: Specializations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(SpecializationViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        Specialization specialization = new Specialization
                        {
                            Id = model.Id,
                            Name = model.Name,
                            CourseId = model.CourseId
                        };
                        _context.Update(specialization);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!SpecializationExists(model.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    Specialization specialization = new Specialization
                    {
                        Name = model.Name,
                        CourseId = model.CourseId
                    };
                    _context.Add(specialization);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            var specializationList = await _context.Specializations.Include(c => c.Course).ToListAsync();
            return View("Index", specializationList);
        }



        // GET: Specializations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (specialization == null)
            {
                return NotFound();
            }
            _context.Specializations.Remove(specialization);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool SpecializationExists(int id)
        {
            return _context.Specializations.Any(e => e.Id == id);
        }
        private async Task LoadDropDowns(SpecializationViewModel model)
        {
            var courses = await _context.Courses.ToListAsync();
            model.Courses = courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();

        }
        public async Task<IActionResult> AddEdit(int? id)
        {
            SpecializationViewModel model = new SpecializationViewModel();
            if (id > 0)
            {
                var specialization = await _context.Specializations.FindAsync(id);
                model = new SpecializationViewModel
                {
                    Id = specialization.Id,
                    Name = specialization.Name,
                    CourseId = specialization.CourseId
                };
            }
            await LoadDropDowns(model);
            return PartialView("_AddEdit", model);
        }
    }
}