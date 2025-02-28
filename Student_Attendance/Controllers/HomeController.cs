using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Student_Attendance.Models;
using Student_Attendance.Data;
using Student_Attendance.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Student_Attendance.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
            : base(context)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> TestAttendanceData()
        {
            try
            {
                var today = DateTime.Today;
                _logger.LogInformation($"Testing attendance data for {today}");

                // Get all attendance records for today
                var todayRecords = await _context.AttendanceRecords
                    .Include(ar => ar.Subject)
                    .Include(ar => ar.Student)
                    .Where(ar => ar.Date.Date == today)
                    .ToListAsync();

                // Group by course for better debugging
                var courseData = await _context.AttendanceRecords
                    .Include(ar => ar.Subject)
                        .ThenInclude(s => s.Course)
                    .Where(ar => ar.Date.Date == today)
                    .GroupBy(ar => ar.Subject.Course)
                    .Select(g => new
                    {
                        CourseId = g.Key.Id,
                        CourseName = g.Key.Name,
                        SubjectsData = g.GroupBy(ar => ar.Subject)
                            .Select(sg => new
                            {
                                SubjectId = sg.Key.Id,
                                SubjectName = sg.Key.Name,
                                SubjectCode = sg.Key.Code,
                                AttendanceCount = sg.Count(),
                                PresentCount = sg.Count(ar => ar.IsPresent),
                                AbsentCount = sg.Count(ar => !ar.IsPresent),
                                Students = sg.Select(ar => new 
                                { 
                                    StudentId = ar.StudentId,
                                    StudentName = ar.Student.Name,
                                    IsPresent = ar.IsPresent
                                }).ToList()
                            }).ToList()
                    })
                    .ToListAsync();

                var debugData = new
                {
                    Date = today,
                    TotalRecords = todayRecords.Count,
                    CourseInfo = courseData,
                    RawRecords = todayRecords.Select(r => new
                    {
                        RecordId = r.Id,
                        StudentId = r.StudentId,
                        StudentName = r.Student.Name,
                        SubjectId = r.SubjectId,
                        SubjectName = r.Subject.Name,
                        IsPresent = r.IsPresent,
                        Timestamp = r.TimeStamp
                    }).ToList()
                };

                return Json(new { success = true, data = debugData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestAttendanceData");
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var today = DateTime.Today;
                _logger.LogInformation($"Loading dashboard for date: {today}");

                var institute = await _context.Institutes.FirstOrDefaultAsync();
                var model = new DashboardViewModel
                {
                    Logo = institute?.Logo ?? "/images/default-logo.png",
                    ShortName = institute?.ShortName ?? "SA",
                    Name = institute?.Name ?? "Student Attendance System",
                    TotalStudents = await _context.Students.CountAsync(s => s.IsActive),
                    TotalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher" && u.IsActive)
                };

                try
                {
                    var anyRecords = await _context.AttendanceRecords
                        .AnyAsync(ar => ar.Date.Date == today);
                    
                    model.HasAttendanceData = anyRecords;

                    if (anyRecords)
                    {
                        var courseAttendance = await _context.AttendanceRecords
                            .Include(ar => ar.Subject)
                                .ThenInclude(s => s.Course)
                            .Where(ar => ar.Date.Date == today)
                            .GroupBy(ar => new { ar.Subject.Course.Id, ar.Subject.Course.Name })
                            .Select(g => new CourseAttendance
                            {
                                CourseId = g.Key.Id,
                                CourseName = g.Key.Name,
                                Subjects = g.GroupBy(ar => new { ar.Subject.Id, ar.Subject.Name, ar.Subject.Code })
                                    .Select(sg => new SubjectAttendanceData
                                    {
                                        SubjectId = sg.Key.Id,
                                        SubjectName = sg.Key.Name,
                                        SubjectCode = sg.Key.Code,
                                        PresentCount = sg.Count(ar => ar.IsPresent),
                                        AbsentCount = sg.Count(ar => !ar.IsPresent)
                                    })
                                    .ToList()
                            })
                            .ToListAsync();

                        model.CourseAttendance = courseAttendance;
                        model.DebugInfo = new DebugInfo
                        {
                            TotalRecords = await _context.AttendanceRecords
                                .CountAsync(ar => ar.Date.Date == today),
                            SubjectsWithAttendance = await _context.AttendanceRecords
                                .Where(ar => ar.Date.Date == today)
                                .Select(ar => ar.SubjectId)
                                .Distinct()
                                .CountAsync(),
                            CoursesWithAttendance = courseAttendance.Count
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching course-wise attendance: {Message}", ex.Message);
                    model.CourseAttendance = new List<CourseAttendance>();
                    ViewBag.AttendanceError = ex.Message;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult Search_Attendance()
        {
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
    }
}