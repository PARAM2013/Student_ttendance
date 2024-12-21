using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;

namespace StudentAttendance.Controllers
{
    public class AcademicYearsController : BaseController
    {
        public AcademicYearsController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: AcademicYears
        public async Task<IActionResult> Index()
        {
            var academicYear = await _context.AcademicYears.ToListAsync();
            return View(academicYear);
        }


        // POST: AcademicYears/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(AcademicYearViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        AcademicYear academicYear = new AcademicYear
                        {
                            Id = model.Id,
                            Name = model.Name,
                            StartDate = model.StartDate,
                            EndDate = model.EndDate,
                            IsActive = model.IsActive
                        };
                        _context.Update(academicYear);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!AcademicYearExists(model.Id))
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
                    AcademicYear academicYear = new AcademicYear
                    {
                        Name = model.Name,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        IsActive = model.IsActive
                    };
                    _context.Add(academicYear);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            var academicYearList = await _context.AcademicYears.ToListAsync();
            return View("Index", academicYearList);
        }



        // GET: AcademicYears/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(m => m.Id == id);
            if (academicYear == null)
            {
                return NotFound();
            }
            _context.AcademicYears.Remove(academicYear);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool AcademicYearExists(int id)
        {
            return _context.AcademicYears.Any(e => e.Id == id);
        }
    }
}