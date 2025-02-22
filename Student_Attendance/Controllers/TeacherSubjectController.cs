using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Authorization;
using Student_Attendance.ViewModels; // Add this line to include the namespace for TeacherSubjectViewModel

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
            var model = new TeacherSubjectViewModel();
            await LoadTeacherSubjectDropDowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Allocate(TeacherSubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.TeacherSubjects
                    .AnyAsync(ts => ts.UserId == model.UserId && 
                                  ts.SubjectId == model.SubjectId && 
                                  ts.AcademicYearId == model.AcademicYearId &&
                                  ts.IsActive);

                if (exists)
                {
                    ModelState.AddModelError("", "This subject is already allocated to the teacher");
                    await LoadTeacherSubjectDropDowns(model);
                    return View(model);
                }

                var allocation = new TeacherSubject
                {
                    UserId = model.UserId,
                    SubjectId = model.SubjectId,
                    AcademicYearId = model.AcademicYearId,
                    IsActive = true
                };

                _context.TeacherSubjects.Add(allocation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await LoadTeacherSubjectDropDowns(model);
            return View(model);
        }

        private async Task LoadTeacherSubjectDropDowns(TeacherSubjectViewModel model)
        {
            // Only get active teachers
            model.Teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new SelectListItem 
                { 
                    Value = u.Id.ToString(), 
                    Text = $"{u.UserName} ({u.Email})" 
                })
                .ToListAsync();

            model.Subjects = await _context.Subjects
                .Include(s => s.Course)
                .Select(s => new SelectListItem 
                { 
                    Value = s.Id.ToString(), 
                    Text = $"{s.Name} ({s.Code}) - {s.Course.Name}" 
                })
                .ToListAsync();

            model.AcademicYears = await _context.AcademicYears
                .Where(ay => ay.IsActive)
                .OrderByDescending(ay => ay.StartDate)
                .Select(ay => new SelectListItem 
                { 
                    Value = ay.Id.ToString(), 
                    Text = ay.Name 
                })
                .ToListAsync();
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
