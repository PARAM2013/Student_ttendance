using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentAttendance.Controllers
{
    public class ClassesController : BaseController
    {
        public ClassesController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Classes
        public async Task<IActionResult> Index()
        {
            var classes = await _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
            return View(classes);
        }


        // POST: Classes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(ClassViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        Class @class = new Class
                        {
                            Id = model.Id,
                            Name = model.Name,
                            CourseId = model.CourseId,
                            AcademicYearId = model.AcademicYearId
                        };
                        _context.Update(@class);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ClassExists(model.Id))
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
                    Class @class = new Class
                    {
                        Name = model.Name,
                        CourseId = model.CourseId,
                        AcademicYearId = model.AcademicYearId
                    };
                    _context.Add(@class);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            var classList = await _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
            return View("Index", classList);
        }



        // GET: Classes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@class == null)
            {
                return NotFound();
            }
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.Id == id);
        }
        private async Task LoadDropDowns(ClassViewModel model)
        {
            var courses = await _context.Courses.ToListAsync();
            model.Courses = courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            var academicYears = await _context.AcademicYears.ToListAsync();
            model.AcademicYears = academicYears.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
        }

        public async Task<IActionResult> AddEdit(int? id)
        {
            ClassViewModel model = new ClassViewModel();
            if (id > 0)
            {
                var @class = await _context.Classes.FindAsync(id);
                model = new ClassViewModel
                {
                    Id = @class.Id,
                    Name = @class.Name,
                    CourseId = @class.CourseId,
                    AcademicYearId = @class.AcademicYearId
                };
            }

            await LoadDropDowns(model);
            return PartialView("_AddEdit", model);
        }
    }
}