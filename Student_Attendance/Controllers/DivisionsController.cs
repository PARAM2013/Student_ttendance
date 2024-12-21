using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentAttendance.Controllers
{
    public class DivisionsController : BaseController
    {
        public DivisionsController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Divisions
        public async Task<IActionResult> Index()
        {
            var divisions = await _context.Divisions.Include(c => c.Class).ToListAsync();
            return View(divisions);
        }


        // POST: Divisions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(DivisionViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        Division division = new Division
                        {
                            Id = model.Id,
                            Name = model.Name,
                            ClassId = model.ClassId
                        };
                        _context.Update(division);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!DivisionExists(model.Id))
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
                    Division division = new Division
                    {
                        Name = model.Name,
                        ClassId = model.ClassId
                    };
                    _context.Add(division);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            var divisionList = await _context.Divisions.Include(c => c.Class).ToListAsync();
            return View("Index", divisionList);
        }



        // GET: Divisions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var division = await _context.Divisions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (division == null)
            {
                return NotFound();
            }
            _context.Divisions.Remove(division);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool DivisionExists(int id)
        {
            return _context.Divisions.Any(e => e.Id == id);
        }
        private async Task LoadDropDowns(DivisionViewModel model)
        {
            var classes = await _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToListAsync();
            model.Classes = classes.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name + "(" + c.Course.Name + "-" + c.AcademicYear.Name + ")" }).ToList();

        }
        public async Task<IActionResult> AddEdit(int? id)
        {
            DivisionViewModel model = new DivisionViewModel();
            if (id > 0)
            {
                var division = await _context.Divisions.FindAsync(id);
                model = new DivisionViewModel
                {
                    Id = division.Id,
                    Name = division.Name,
                    ClassId = division.ClassId
                };
            }
            await LoadDropDowns(model);
            return PartialView("_AddEdit", model);
        }
    }
}