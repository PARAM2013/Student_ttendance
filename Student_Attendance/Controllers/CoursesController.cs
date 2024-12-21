using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using Student_Attendance.ViewModels;

namespace StudentAttendance.Controllers
{
    public class CoursesController : BaseController
    {
        public CoursesController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }


        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEdit(CourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        Course course = new Course
                        {
                            Id = model.Id,
                            Name = model.Name
                        };
                        _context.Update(course);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!CourseExists(model.Id))
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
                    Course course = new Course
                    {
                        Name = model.Name
                    };
                    _context.Add(course);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));

            }
            var courseList = await _context.Courses.ToListAsync();
            return View("Index", courseList);
        }



        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}