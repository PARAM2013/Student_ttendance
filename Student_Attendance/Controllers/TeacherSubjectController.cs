using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Student_Attendance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherSubjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeacherSubjectController> _logger;

        public TeacherSubjectController(ApplicationDbContext context, ILogger<TeacherSubjectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var allocations = await _context.TeacherSubjects
                .Include(ts => ts.User)
                .Include(ts => ts.Subject)
                .Include(ts => ts.AcademicYear)
                .Where(ts => ts.IsActive)
                .OrderBy(ts => ts.User.UserName)
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
                try
                {
                    // Check if allocation already exists
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

                    var teacherSubject = new TeacherSubject
                    {
                        UserId = model.UserId,
                        SubjectId = model.SubjectId,
                        AcademicYearId = model.AcademicYearId,
                        IsActive = true
                    };

                    _context.TeacherSubjects.Add(teacherSubject);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error allocating subject to teacher");
                    ModelState.AddModelError("", "Error allocating subject to teacher");
                }
            }

            await LoadTeacherSubjectDropDowns(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Deallocate(int id)
        {
            try
            {
                var allocation = await _context.TeacherSubjects.FindAsync(id);
                if (allocation == null)
                {
                    return Json(new { success = false, message = "Allocation not found" });
                }

                allocation.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Subject deallocated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deallocating subject");
                return Json(new { success = false, message = "Error deallocating subject" });
            }
        }

        private async Task LoadTeacherSubjectDropDowns(TeacherSubjectViewModel model)
        {
            model.Teachers = await _context.Users
                .Where(u => u.Role == "Teacher")
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
    }
}
