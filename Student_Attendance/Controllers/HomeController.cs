using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Student_Attendance.ViewModels;
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
                var model = new DashboardStatsViewModel();

                // Basic setup
                var institute = await _context.Institutes.FirstOrDefaultAsync();
                model.Logo = institute?.Logo ?? "/images/default-logo.png";
                model.ShortName = institute?.ShortName ?? "SA";
                model.Name = institute?.Name ?? "Student Attendance System";
                
                // Get basic counts
                model.TotalStudents = await _context.Students.CountAsync(s => s.IsActive);
                model.TotalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher" && u.IsActive);

                // Get recent activities
                model.RecentActivities = await _context.AttendanceRecords
                    .Include(ar => ar.MarkedBy)
                    .Include(ar => ar.Subject)
                    .OrderByDescending(ar => ar.TimeStamp)
                    .Take(5)
                    .Select(ar => new ActivityLogSummary
                    {
                        Timestamp = ar.TimeStamp,
                        Action = "Marked Attendance",
                        UserName = ar.MarkedBy.UserName,
                        Details = $"for {ar.Subject.Name}"
                    })
                    .ToListAsync();

                // Get low attendance students
                model.LowAttendanceStudents = await GetLowAttendanceStudents();

                // Get weekly trends
                model.WeeklyTrends = await GetWeeklyTrends();

                // Regular attendance data
                await LoadTodayAttendanceData(model, today);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View("Error");
            }
        }

        private async Task<List<StudentAttendanceAlert>> GetLowAttendanceStudents()
        {
            var threshold = 75.0; // 75% attendance threshold
            
            return await _context.Students
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    Student = s,
                    AttendancePercent = s.AttendanceRecords.Count() > 0 
                        ? s.AttendanceRecords.Count(ar => ar.IsPresent) * 100.0 / s.AttendanceRecords.Count() 
                        : 0
                })
                .Where(x => x.AttendancePercent < threshold)
                .OrderBy(x => x.AttendancePercent)
                .Take(5)
                .Select(x => new StudentAttendanceAlert
                {
                    StudentId = x.Student.Id,
                    StudentName = x.Student.Name,
                    Course = x.Student.Course.Name,
                    AttendancePercentage = x.AttendancePercent
                })
                .ToListAsync();
        }

        private async Task<WeeklyAttendanceData> GetWeeklyTrends()
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            var trends = await _context.AttendanceRecords
                .Where(ar => last7Days.Contains(ar.Date.Date))
                .GroupBy(ar => ar.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Present = g.Count(ar => ar.IsPresent),
                    Total = g.Count()
                })
                .ToListAsync();

            return new WeeklyAttendanceData
            {
                Dates = last7Days.Select(d => d.ToString("MMM dd")).ToList(),
                Percentages = last7Days.Select(d =>
                {
                    var dayTrend = trends.FirstOrDefault(t => t.Date == d);
                    return dayTrend != null && dayTrend.Total > 0
                        ? Math.Round(dayTrend.Present * 100.0 / dayTrend.Total, 1)
                        : 0;
                }).ToList()
            };
        }

        private async Task LoadTodayAttendanceData(DashboardStatsViewModel model, DateTime today)
        {
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
        }

        [AllowAnonymous]
        public IActionResult Search_Attendance()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchStudents(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
            {
                return Json(new List<object>());
            }

            try
            {
                // Query your database for students matching the search term with proper Include
                var students = _context.Students
                    .Include(s => s.Class)
                    .Where(s => s.Name.Contains(searchTerm) && s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        className = s.Class != null ? s.Class.Name : "No Class"
                    })
                    .Take(10)
                    .ToList();

                return Json(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for students: {Message}", ex.Message);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetStudentAttendance(int studentId)
        {
            try
            {
                // Get student details
                var student = _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Get attendance records - changed from Attendance to AttendanceRecords
                var attendanceRecords = _context.AttendanceRecords
                    .Where(a => a.StudentId == studentId)
                    .ToList();

                // Calculate overall attendance - fixed Count() method call
                int totalClasses = attendanceRecords.Count;
                int attendedClasses = attendanceRecords.Count(a => a.IsPresent);
                double overallAttendance = totalClasses > 0 
                    ? (double)attendedClasses / totalClasses * 100 
                    : 0;

                // Get subject-wise attendance - changed from Attendance to AttendanceRecords
                var subjectAttendance = _context.AttendanceRecords
                    .Where(a => a.StudentId == studentId)
                    .Include(a => a.Subject)
                    .GroupBy(a => a.Subject.Name)
                    .Select(g => new
                    {
                        subjectName = g.Key,
                        total = g.Count(),
                        present = g.Count(a => a.IsPresent)
                    })
                    .ToList();

                // Calculate monthly attendance for trend chart - changed from Attendance to AttendanceRecords
                var monthlyAttendance = _context.AttendanceRecords
                    .Where(a => a.StudentId == studentId)
                    .GroupBy(a => new { Month = a.Date.Month, Year = a.Date.Year })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month) + " " + g.Key.Year,
                        total = g.Count(),
                        present = g.Count(a => a.IsPresent),
                        percentage = g.Count() > 0 ? (double)g.Count(a => a.IsPresent) / g.Count() * 100 : 0
                    })
                    .ToList();

                // Calculate weekly attendance pattern - changed from Attendance to AttendanceRecords
                var attendanceForWeeklyPattern = _context.AttendanceRecords
                    .Where(a => a.StudentId == studentId)
                    .ToList(); // Execute the query first to bring data into memory

                var weeklyAttendance = attendanceForWeeklyPattern
                    .GroupBy(a => a.Date.DayOfWeek)
                    .ToDictionary(
                        g => g.Key.ToString().Substring(0, 3), // Get first 3 letters of day name
                        g => new
                        {
                            total = g.Count(),
                            present = g.Count(a => a.IsPresent)
                        }
                    );

                return Json(new
                {
                    success = true,
                    studentName = student.Name,
                    className = student.Class.Name,
                    totalClasses = totalClasses,
                    attendedClasses = attendedClasses,
                    overallAttendance = overallAttendance,
                    subjectAttendance = subjectAttendance,
                    monthlyAttendance = monthlyAttendance,
                    weeklyAttendance = weeklyAttendance
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}