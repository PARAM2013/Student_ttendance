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
            
            // Get current date
            var today = DateTime.Today;

            // Calculate statistics
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            
            // Get today's attendance
            var todayAttendance = await _context.AttendanceRecords
                .Where(a => a.Date.Date == today)
                .ToListAsync();
            
            ViewBag.PresentToday = todayAttendance.Count(a => a.IsPresent);
            ViewBag.AbsentToday = todayAttendance.Count(a => !a.IsPresent);

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
