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
            var model = new ClassViewModel();
            await LoadDropDowns(model);
            ViewBag.Courses = model.Courses;
            ViewBag.AcademicYears = model.AcademicYears;
            return View(classes);
        }


        // POST: Classes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(ClassViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                       .Where(x => x.Value.Errors.Count > 0)
                       .ToDictionary(
                           kvp => kvp.Key,
                           kvp => kvp.Value.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid value"
                       );
                    var classList = await _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
                    var viewModel = new ClassViewModel();
                    await LoadDropDowns(viewModel);
                    ViewBag.Courses = viewModel.Courses;
                    ViewBag.AcademicYears = viewModel.AcademicYears;
                    return View("Index", classList);

                }


                if (model.Id > 0)
                {
                    // Update existing class
                    var existingClass = await _context.Classes.FindAsync(model.Id);
                    if (existingClass == null)
                    {
                        return NotFound();
                    }

                    existingClass.Name = model.Name;
                    existingClass.CourseId = model.CourseId;
                    existingClass.AcademicYearId = model.AcademicYearId;
                    _context.Update(existingClass);
                }
                else
                {
                    // Create new class
                    var newClass = new Class
                    {
                        Name = model.Name,
                        CourseId = model.CourseId,
                        AcademicYearId = model.AcademicYearId
                    };
                    _context.Add(newClass);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var classList = await _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
                var viewModel = new ClassViewModel();
                await LoadDropDowns(viewModel);
                ViewBag.Courses = viewModel.Courses;
                ViewBag.AcademicYears = viewModel.AcademicYears;
                // Log the exception here
                ModelState.AddModelError("general", "Error occurred while saving the record " + ex.Message);
                return View("Index", classList);
            }
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
    }
}