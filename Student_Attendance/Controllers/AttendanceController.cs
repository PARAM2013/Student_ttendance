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
                        MarkedById = User.Identity?.Name ?? "Unknown"
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

        [HttpGet]
        public async Task<IActionResult> GetStudentsByDivision(int subjectId, int divisionId, DateTime date)
        {
            try
            {
                // Check if teacher is authorized for this subject
                if (!User.IsInRole("Admin"))
                {
                    var hasPermission = await _context.TeacherSubjects
                        .AnyAsync(ts => ts.UserId == CurrentUser.Id && 
                                       ts.SubjectId == subjectId && 
                                       ts.IsActive);
                    if (!hasPermission)
                    {
                        return Json(new { success = false, message = "You are not authorized to take attendance for this subject." });
                    }
                }

                // Get students who are mapped to this subject
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
                    return Json(new { success = false, message = "No students found for the selected criteria." });
                }

                // Get existing attendance records if any
                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == subjectId && 
                               a.Date.Date == date.Date)
                    .ToListAsync();

                // Update attendance status based on existing records
                foreach (var student in students)
                {
                    var record = existingRecords.FirstOrDefault(a => a.StudentId == student.StudentId);
                    if (record != null)
                    {
                        student.IsPresent = record.IsPresent;
                    }
                }

                return PartialView("_StudentList", students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students for attendance");
                return Json(new { success = false, message = "An error occurred while loading students." });
            }
        }

        // GET: Bulk Attendance page
        [HttpGet]
        public IActionResult BulkAttendance()
        {
            // Prepare dropdown lists (for Admin and Teacher)
            var model = new BulkAttendanceViewModel
            {
                // If teacher, set logged-in teacher and allocated subjects; else, load all teachers.
                Teachers = new SelectList(User.IsInRole("Admin")
                    ? _context.Users.Where(u => u.Role=="Teacher").ToList()
                    : new List<User>(), "Id", "UserName"),
                // For teacher login, teacher is read-only.
                SelectedTeacherId = User.IsInRole("Teacher") ? CurrentUser.Id : 0,
                Divisions = new SelectList(_context.Divisions.ToList(), "Id", "Name")
                // ...populate other dropdowns if needed...
            };
            return View(model);
        }

        // GET: Get Bulk Attendance sheet (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetBulkAttendanceSheet(int teacherId, int subjectId, int divisionId, int month, int year)
        {
            // Get list of dates for given month and year
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var dates = Enumerable.Range(1, daysInMonth)
                            .Select(d => new DateTime(year, month, d))
                            .ToList();

            // Get students for division
            var students = await _context.Students
                                .Where(s => s.DivisionId == divisionId && s.IsActive)
                                .Select(s => new BulkAttendanceStudentViewModel {
                                    StudentId = s.Id,
                                    EnrollmentNo = s.EnrollmentNo,
                                    StudentName = s.Name
                                })
                                .ToListAsync();

            // Get existing attendance records for the month and subject
            var records = await _context.AttendanceRecords
                                .Where(a => a.SubjectId == subjectId &&
                                        a.Date.Month == month &&
                                        a.Date.Year == year)
                                .ToListAsync();

            // Pass data to grid view model (assemble a dictionary of attendance status)
            var gridModel = new BulkAttendanceGridViewModel {
                Dates = dates,
                Students = students,
                ExistingAttendance = records.ToDictionary(
                                        r => (r.StudentId, r.Date.Date),
                                        r => r.IsPresent)
            };

            return PartialView("_BulkAttendanceGrid", gridModel);
        }

        // POST: Save Bulk Attendance
        [HttpPost]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] BulkAttendanceSaveModel model)
        {
            try
            {
                // Remove existing records for the subject and month
                var month = model.Month;
                var year = model.Year;
                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == model.SubjectId &&
                                a.Date.Month == month &&
                                a.Date.Year == year)
                    .ToListAsync();
                _context.AttendanceRecords.RemoveRange(existingRecords);

                // Loop over attendance data from grid
                foreach(var entry in model.AttendanceData)
                {
                    // entry.Key = studentId, entry.Value = dictionary of date and presence flag
                    int studentId = entry.Key;
                    foreach(var datePair in entry.Value)
                    {
                        var attendance = new AttendanceRecord
                        {
                            StudentId = studentId,
                            SubjectId = model.SubjectId,
                            Date = datePair.Key,
                            IsPresent = datePair.Value,
                            TimeStamp = DateTime.Now,
                            MarkedById = User.Identity?.Name ?? "Unknown"
                        };
                        _context.AttendanceRecords.Add(attendance);
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bulk attendance saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
