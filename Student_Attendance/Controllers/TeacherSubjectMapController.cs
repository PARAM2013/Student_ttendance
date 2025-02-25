using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;

namespace Student_Attendance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherSubjectMapController : BaseController
    {
        public TeacherSubjectMapController(ApplicationDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            var mappings = await _context.TeacherSubjects
                .Include(ts => ts.User)
                .Include(ts => ts.Subject)
                .Include(ts => ts.AcademicYear)
                .Where(ts => ts.IsActive)
                .ToListAsync();
            return View(mappings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Map(int teacherId, int subjectId, int academicYearId)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.TeacherSubjects
                    .AnyAsync(ts => ts.UserId == teacherId && 
                                  ts.SubjectId == subjectId && 
                                  ts.AcademicYearId == academicYearId &&
                                  ts.IsActive);

                if (exists)
                {
                    return Json(new { success = false, message = "This mapping already exists" });
                }

                var mapping = new TeacherSubject
                {
                    UserId = teacherId,
                    SubjectId = subjectId,
                    AcademicYearId = academicYearId,
                    IsActive = true
                };

                _context.TeacherSubjects.Add(mapping);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public async Task<IActionResult> Unmap(int id)
        {
            var mapping = await _context.TeacherSubjects.FindAsync(id);
            if (mapping != null)
            {
                mapping.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(t => new { id = t.Id, name = t.UserName })
                .ToListAsync();
            return Json(teachers);
        }

        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Select(s => new { id = s.Id, name = $"{s.Name} ({s.Code})" })
                .ToListAsync();
            return Json(subjects);
        }
    }
}
