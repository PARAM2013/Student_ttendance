using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentAttendance.Controllers
{
    public class SubjectsController : BaseController
    {
        public SubjectsController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects.Include(c => c.Specialization).Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
            return View(subjects);
        }


        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(SubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        Subject subject = new Subject
                        {
                            Id = model.Id,
                            Name = model.Name,
                            Code = model.Code,
                            SpecializationId = model.SpecializationId,
                            Semester = model.Semester,
                            CourseId = model.CourseId,
                            AcademicYearId = model.AcademicYearId
                        };
                        _context.Update(subject);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!SubjectExists(model.Id))
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
                    Subject subject = new Subject
                    {
                        Name = model.Name,
                        Code = model.Code,
                        SpecializationId = model.SpecializationId,
                        Semester = model.Semester,
                        CourseId = model.CourseId,
                        AcademicYearId = model.AcademicYearId
                    };
                    _context.Add(subject);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            await LoadDropDowns(model);
            return View("Index", await _context.Subjects.Include(c => c.Specialization).Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync());
        }



        // GET: Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (subject == null)
            {
                return NotFound();
            }
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }
        private async Task LoadDropDowns(SubjectViewModel model)
        {
            var specializations = await _context.Specializations.ToListAsync();
            model.Specializations = specializations.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            var courses = await _context.Courses.ToListAsync();
            model.Courses = courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            var academicYears = await _context.AcademicYears.ToListAsync();
            model.AcademicYears = academicYears.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
        }

        public async Task<IActionResult> AddEdit(int? id)
        {
            SubjectViewModel model = new SubjectViewModel();
            if (id > 0)
            {
                var subject = await _context.Subjects.FindAsync(id);
                model = new SubjectViewModel
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    SpecializationId = subject.SpecializationId,
                    Semester = subject.Semester,
                    CourseId = subject.CourseId,
                    AcademicYearId = subject.AcademicYearId

                };
            }
            await LoadDropDowns(model);
            return PartialView("_AddEdit", model);
        }
    }
}