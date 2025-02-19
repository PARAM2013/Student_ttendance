using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Authorization;

namespace Student_Attendance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherSubjectController : BaseController
    {
        private readonly ILogger<TeacherSubjectController> _logger;

        public TeacherSubjectController(ApplicationDbContext context, ILogger<TeacherSubjectController> logger) 
            : base(context)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var allocations = await _context.TeacherSubjects
                .Include(ts => ts.User)
                .Include(ts => ts.Subject)
                .Include(ts => ts.AcademicYear)
                .Where(ts => ts.IsActive)
                .ToListAsync();

            return View(allocations);
        }

        public async Task<IActionResult> Allocate()
        {
            ViewBag.Teachers = new SelectList(await _context.Users
                .Where(u => u.Role == "Teacher")
                .ToListAsync(), "Id", "UserName");
                
            ViewBag.Subjects = new SelectList(await _context.Subjects
                .ToListAsync(), "Id", "Name");
                
            ViewBag.AcademicYears = new SelectList(await _context.AcademicYears
                .Where(ay => ay.IsActive)
                .ToListAsync(), "Id", "Name");

            return View(new TeacherSubject());
        }

        [HttpPost]
        public async Task<IActionResult> Allocate(TeacherSubject allocation)
        {
            if (ModelState.IsValid)
            {
                // Check if allocation already exists
                var exists = await _context.TeacherSubjects
                    .AnyAsync(ts => ts.UserId == allocation.UserId && 
                                  ts.SubjectId == allocation.SubjectId && 
                                  ts.AcademicYearId == allocation.AcademicYearId &&
                                  ts.IsActive);

                if (exists)
                {
                    ModelState.AddModelError("", "This subject is already allocated to the teacher");
                    return View(allocation);
                }

                allocation.IsActive = true;
                _context.TeacherSubjects.Add(allocation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(allocation);
        }

        [HttpPost]
        public async Task<IActionResult> Deallocate(int id)
        {
            var allocation = await _context.TeacherSubjects.FindAsync(id);
            if (allocation != null)
            {
                allocation.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherSubjects(int teacherId)
        {
            var subjects = await _context.TeacherSubjects
                .Include(ts => ts.Subject)
                .Where(ts => ts.UserId == teacherId && ts.IsActive)
                .Select(ts => new { id = ts.SubjectId, name = ts.Subject.Name })
                .ToListAsync();

            return Json(subjects);
        }
    }
}
