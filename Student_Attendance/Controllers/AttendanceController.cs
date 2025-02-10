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
    public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Attendance/Take
    public async Task<IActionResult> Take()
    {
        var model = new AttendanceViewModel
        {
            Date = DateTime.Today,
            Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name"),
            Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
        };
        return View(model);
    }

    // GET: /Attendance/GetStudentsByDivision
    [HttpGet]
    public async Task<IActionResult> GetStudentsByDivision(int divisionId, int subjectId, DateTime date)
    {
        try
        {
            // Get students with subject mapping
            var students = await _context.Students
                .Include(s => s.StudentSubjects)
                .Where(s => s.DivisionId == divisionId && 
                           s.IsActive && 
                           s.StudentSubjects.Any(ss => ss.SubjectId == subjectId))
                .Select(s => new StudentAttendanceViewModel
                {
                    StudentId = s.Id,
                    EnrollmentNo = s.EnrollmentNo,
                    StudentName = s.Name,
                    IsPresent = true // Default to present
                })
                .ToListAsync();

            if (!students.Any())
            {
                return Json(new { success = false, message = "No students found for selected criteria" });
            }

            // Get existing attendance
            var existingAttendance = await _context.AttendanceRecords
                .Where(a => a.Date.Date == date.Date && 
                           a.SubjectId == subjectId)
                .ToListAsync();

            // Update attendance status if exists
            foreach (var attendance in existingAttendance)
            {
                var student = students.FirstOrDefault(s => s.StudentId == attendance.StudentId);
                if (student != null)
                {
                    student.IsPresent = attendance.IsPresent;
                    student.AbsenceReason = attendance.AbsenceReason;
                }
            }

            return PartialView("_StudentList", students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading students for division {DivisionId}", divisionId);
            return Json(new { success = false, message = "Error loading students: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAttendance(AttendanceViewModel model)
    {
        try
        {
            // Remove existing attendance records for this date and subject
            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.Date.Date == model.Date.Date && 
                           a.SubjectId == model.SubjectId)
                .ToListAsync();

            _context.AttendanceRecords.RemoveRange(existingRecords);

            // Add new attendance records
            foreach (var student in model.Students)
            {
                var attendance = new AttendanceRecord
                {
                    StudentId = student.StudentId,
                    SubjectId = model.SubjectId,
                    Date = model.Date,
                    IsPresent = student.IsPresent,
                    AbsenceReason = student.AbsenceReason,
                    TimeStamp = DateTime.Now,
                    MarkedById = User.Identity.Name
                };
                _context.AttendanceRecords.Add(attendance);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Attendance marked successfully" });
        }
        catch (Exception ex)
        {
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

    public async Task<IActionResult> View()
    {
        var model = new AttendanceViewModel
        {
            Date = DateTime.Today,
            Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name"),
            Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
        };
        return View(model);
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
                    student.AbsenceReason = record.AbsenceReason;
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
