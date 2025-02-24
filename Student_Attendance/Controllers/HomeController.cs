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

            // Get course-wise statistics
            var courseStats = await _context.Courses
                .Select(c => new
                {
                    CourseName = c.Name,
                    TotalStudents = c.Students.Count(),
                    TotalTeachers = c.Subjects.SelectMany(s => s.TeacherSubjects).Select(ts => ts.UserId).Distinct().Count(),
                    TodayAttendance = c.Students
                        .SelectMany(s => s.StudentSubjects)
                        .SelectMany(ss => ss.Subject.AttendanceRecords)
                        .Where(ar => ar.Date.Date == today)
                        .GroupBy(ar => ar.StudentId)
                        .Select(g => g.First())
                        .Count(ar => ar.IsPresent),
                    TodayAbsent = c.Students
                        .SelectMany(s => s.StudentSubjects)
                        .SelectMany(ss => ss.Subject.AttendanceRecords)
                        .Where(ar => ar.Date.Date == today)
                        .GroupBy(ar => ar.StudentId)
                        .Select(g => g.First())
                        .Count(ar => !ar.IsPresent)
                })
                .ToListAsync();

            // Get class-wise statistics
            var classStats = await _context.Classes
                .Select(c => new
                {
                    ClassName = c.Name,
                    TotalStudents = c.Students.Count(),
                    TodayAttendance = c.Students
                        .SelectMany(s => s.StudentSubjects)
                        .SelectMany(ss => ss.Subject.AttendanceRecords)
                        .Where(ar => ar.Date.Date == today)
                        .GroupBy(ar => ar.StudentId)
                        .Select(g => g.First())
                        .Count(ar => ar.IsPresent)
                })
                .ToListAsync();

            ViewBag.CourseStats = courseStats;
            ViewBag.ClassStats = classStats;

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
