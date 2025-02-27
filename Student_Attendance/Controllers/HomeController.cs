using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Student_Attendance.Models;
using Student_Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Student_Attendance.Controllers
{
    [Authorize]  // Add this to require authentication for all actions
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var institute = await _context.Institutes.FirstOrDefaultAsync();
            var today = DateTime.Today;

            // Basic stats
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher");

            // Get subject-wise attendance data with proper joins
            var courseAttendance = await _context.Courses
                .Select(c => new
                {
                    CourseId = c.Id,
                    CourseName = c.Name,
                    Subjects = c.Subjects
                        .Select(s => new
                        {
                            SubjectName = s.Name,
                            SubjectCode = s.Code,
                            TotalStudents = s.StudentSubjects.Count(),
                            AttendanceData = s.StudentSubjects
                                .SelectMany(ss => ss.Student.AttendanceRecords
                                    .Where(ar => ar.SubjectId == s.Id && ar.Date.Date == today)
                                    .Select(ar => new { ar.IsPresent }))
                                .ToList()
                        })
                        .Select(s => new
                        {
                            s.SubjectName,
                            s.SubjectCode,
                            s.TotalStudents,
                            PresentCount = s.AttendanceData.Count(a => a.IsPresent),
                            AbsentCount = s.AttendanceData.Count(a => !a.IsPresent)
                        })
                        .Where(s => s.PresentCount + s.AbsentCount > 0)
                        .ToList()
                })
                .Where(c => c.Subjects.Any())
                .ToListAsync();

            ViewBag.CourseAttendance = courseAttendance;
            
            return View(institute);
        }

        [AllowAnonymous] // Add this attribute to allow access without authentication
        public IActionResult Search_Attendance()
        {
            // If user is already authenticated, redirect to Index
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
