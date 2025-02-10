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
}
}
