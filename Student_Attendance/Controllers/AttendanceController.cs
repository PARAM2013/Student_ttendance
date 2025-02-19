using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Student_Attendance.Data;
using Student_Attendance.ViewModels;
using Student_Attendance.Models;

namespace Student_Attendance.Controllers
{
    [Authorize]
    public class AttendanceController : BaseController
    {
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Take()
        {
            var model = new AttendanceViewModel
            {
                Date = DateTime.Today,
                Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
            };

            // If user is admin, load all subjects
            if (User.IsInRole("Admin"))
            {
                model.Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name");
                ViewBag.Teachers = new SelectList(await _context.Users
                    .Where(u => u.Role == "Teacher")
                    .ToListAsync(), "Id", "UserName");
            }
            else
            {
                // For teachers, only load allocated subjects
                var allocatedSubjects = await _context.TeacherSubjects
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.UserId == CurrentUser.Id && ts.IsActive)
                    .Select(ts => ts.Subject)
                    .ToListAsync();
                model.Subjects = new SelectList(allocatedSubjects, "Id", "Name");
            }

            return View(model);
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

        // Update existing methods to check permissions
        [HttpPost]
        public async Task<IActionResult> MarkAttendance(AttendanceViewModel model)
        {
            try
            {
                // Verify user has permission for this subject
                if (!User.IsInRole("Admin"))
                {
                    var hasPermission = await _context.TeacherSubjects
                        .AnyAsync(ts => ts.UserId == CurrentUser.Id && 
                                      ts.SubjectId == model.SubjectId && 
                                      ts.IsActive);
                    if (!hasPermission)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }
                }

                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.Date.Date == model.Date.Date && 
                               a.SubjectId == model.SubjectId)
                    .ToListAsync();

                _context.AttendanceRecords.RemoveRange(existingRecords);

                foreach (var student in model.Students)
                {
                    var attendance = new AttendanceRecord
                    {
                        StudentId = student.StudentId,
                        SubjectId = model.SubjectId,
                        Date = model.Date,
                        IsPresent = student.IsPresent,
                        TimeStamp = DateTime.Now,
                        MarkedById = User.Identity?.Name
                    };
                    _context.AttendanceRecords.Add(attendance);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attendance marked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                return Json(new { success = false, message = "Error marking attendance" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var model = new AttendanceReportViewModel
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today,
                Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name"),
                Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(int subjectId, int divisionId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get all students in the division
                var students = await _context.Students
                    .Where(s => s.DivisionId == divisionId && s.IsActive)
                    .Select(s => new { s.Id, s.EnrollmentNo, s.Name })
                    .ToListAsync();

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No students found in this division" });
                }

                // Get attendance records for the date range
                var attendanceData = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == subjectId &&
                               a.Date.Date >= startDate.Date &&
                               a.Date.Date <= endDate.Date &&
                               students.Select(s => s.Id).Contains(a.StudentId))
                    .GroupBy(a => a.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalClasses = g.Count(),
                        Present = g.Count(a => a.IsPresent),
                        Absent = g.Count(a => !a.IsPresent)
                    })
                    .ToListAsync();

                var report = students.Select(s =>
                {
                    var data = attendanceData.FirstOrDefault(a => a.StudentId == s.Id);
                    return new AttendanceReportItemViewModel
                    {
                        StudentId = s.Id,
                        EnrollmentNo = s.EnrollmentNo,
                        StudentName = s.Name,
                        TotalClasses = data?.TotalClasses ?? 0,
                        Present = data?.Present ?? 0,
                        Absent = data?.Absent ?? 0,
                        AttendancePercentage = data?.TotalClasses > 0 
                            ? (decimal)data.Present / data.TotalClasses * 100 
                            : 0
                    };
                }).ToList();

                return PartialView("_AttendanceReport", report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return Json(new { success = false, message = "Error generating report" });
            }
        }

        // Add 'new' keyword to resolve the warning
        public new async Task<IActionResult> View()
        {
            var model = new AttendanceViewModel
            {
                Date = DateTime.Today,
                Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name"),
                Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
            };
            return base.View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceByDate(int subjectId, int divisionId, DateTime date)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.DivisionId == divisionId && s.IsActive)
                    .Select(s => new StudentAttendanceViewModel
                    {
                        StudentId = s.Id,
                        EnrollmentNo = s.EnrollmentNo,
                        StudentName = s.Name,
                        IsPresent = true
                    })
                    .ToListAsync();

                var attendanceRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == subjectId && 
                               a.Date.Date == date.Date)
                    .ToListAsync();

                foreach (var student in students)
                {
                    var record = attendanceRecords.FirstOrDefault(a => a.StudentId == student.StudentId);
                    if (record != null)
                    {
                        student.IsPresent = record.IsPresent;
                    }
                }

                return PartialView("_AttendanceView", students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attendance");
                return Json(new { success = false, message = "Error loading attendance data" });
            }
        }
    }
}
