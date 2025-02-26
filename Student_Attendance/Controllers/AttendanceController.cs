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
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Layout.Borders;
using iText.Kernel.Events;  // Add this at the top with other using statements
using iText.Kernel.Pdf.Canvas;  // Add this for PdfCanvas

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
        public async Task<IActionResult> MarkAttendance([FromBody] AttendanceViewModel model)
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

                // Create audit records for modifications and update existing records
                foreach (var student in model.Students)
                {
                    var existingRecord = existingRecords.FirstOrDefault(r => r.StudentId == student.StudentId);
                    
                    if (existingRecord != null)
                    {
                        // If record exists and status changed, create audit and update record
                        if (existingRecord.IsPresent != student.IsPresent)
                        {
                            var audit = new AttendanceAudit
                            {
                                AttendanceRecordId = existingRecord.Id,
                                ModifiedById = CurrentUser.Id,
                                ModifiedOn = DateTime.Now,
                                OldValue = existingRecord.IsPresent,
                                NewValue = student.IsPresent
                            };
                            _context.AttendanceAudits.Add(audit);
                            
                            // Update existing record
                            existingRecord.IsPresent = student.IsPresent;
                            existingRecord.TimeStamp = DateTime.Now;
                        }
                        // Update discussion topic if changed
                        if (existingRecord.DiscussionTopic != model.DiscussionTopic)
                        {
                            existingRecord.DiscussionTopic = model.DiscussionTopic;
                        }
                    }
                    else
                    {
                        // Create new record if it doesn't exist
                        var attendance = new AttendanceRecord
                        {
                            StudentId = student.StudentId,
                            SubjectId = model.SubjectId,
                            Date = model.Date,
                            IsPresent = student.IsPresent,
                            TimeStamp = DateTime.Now,
                            MarkedById = CurrentUser.Id,
                            DiscussionTopic = model.DiscussionTopic
                        };
                        _context.AttendanceRecords.Add(attendance);
                    }
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

        // Add new method to view audit trail
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditTrail(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AttendanceAudits
                .Include(a => a.AttendanceRecord)
                    .ThenInclude(ar => ar.Student)
                .Include(a => a.AttendanceRecord)
                    .ThenInclude(ar => ar.Subject)
                .Include(a => a.ModifiedBy)
                .OrderByDescending(a => a.ModifiedOn)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.ModifiedOn.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(a => a.ModifiedOn.Date <= endDate.Value.Date);

            var audits = await query.ToListAsync();
            return View(audits);
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
                // Check if teacher is authorized and active
                if (!User.IsInRole("Admin"))
                {
                    var teacher = await _context.Users.FindAsync(CurrentUser.Id);
                    if (teacher == null || !teacher.IsActive)
                    {
                        return Json(new { success = false, message = "Your account is not active." });
                    }

                    var hasPermission = await _context.TeacherSubjects
                        .AnyAsync(ts => ts.UserId == CurrentUser.Id && 
                                       ts.SubjectId == subjectId && 
                                       ts.IsActive);
                    if (!hasPermission)
                    {
                        return Json(new { success = false, message = "You are not authorized to take attendance for this subject." });
                    }
                }

                // Get existing discussion topic for this date and subject
                var existingDiscussionTopic = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == subjectId && 
                               a.Date.Date == date.Date)
                    .Select(a => a.DiscussionTopic)
                    .FirstOrDefaultAsync();

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
                        IsPresent = true, // Default to present
                        DiscussionTopic = existingDiscussionTopic // Add the discussion topic
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

                ViewBag.ExistingDiscussionTopic = existingDiscussionTopic;
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
        public IActionResult BulkAttendanceMonthly()
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

        [HttpGet]
        public IActionResult BulkAttendance()
        {
            var model = new BulkAttendanceRangeViewModel
            {
                Teachers = new SelectList(User.IsInRole("Admin")
                    ? _context.Users.Where(u => u.Role=="Teacher").ToList()
                    : new List<User>(), "Id", "UserName"),
                SelectedTeacherId = User.IsInRole("Teacher") ? CurrentUser.Id : 0,
                Divisions = new SelectList(_context.Divisions.ToList(), "Id", "Name"),
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
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

            // Get only students who are mapped to this subject
            var students = await _context.Students
                                .Include(s => s.StudentSubjects)
                                .Where(s => s.DivisionId == divisionId && 
                                          s.IsActive &&
                                          s.StudentSubjects.Any(ss => ss.SubjectId == subjectId))
                                .Select(s => new BulkAttendanceStudentViewModel {
                                    StudentId = s.Id,
                                    EnrollmentNo = s.EnrollmentNo,
                                    StudentName = s.Name
                                })
                                .ToListAsync();

            if (!students.Any())
            {
                return Json(new { success = false, message = "No students found mapped to this subject." });
            }

            // Get existing attendance records
            var records = await _context.AttendanceRecords
                                .Where(a => a.SubjectId == subjectId &&
                                        a.Date.Month == month &&
                                        a.Date.Year == year)
                                .ToListAsync();

            var gridModel = new BulkAttendanceGridViewModel {
                Dates = dates,
                Students = students,
                ExistingAttendance = records.ToDictionary(
                                        r => (r.StudentId, r.Date.Date),
                                        r => r.IsPresent)
            };

            return PartialView("_BulkAttendanceGrid", gridModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetBulkAttendanceSheetRange(int teacherId, int subjectId, int divisionId, DateTime startDate, DateTime endDate)
        {
            if ((endDate - startDate).Days > 31)
            {
                return Json(new { success = false, message = "Date range cannot exceed 31 days" });
            }

            var dates = Enumerable.Range(0, (endDate - startDate).Days + 1)
                            .Select(d => startDate.AddDays(d))
                            .ToList();

            // Get only students who are mapped to this subject
            var students = await _context.Students
                                .Include(s => s.StudentSubjects)
                                .Where(s => s.DivisionId == divisionId && 
                                          s.IsActive &&
                                          s.StudentSubjects.Any(ss => ss.SubjectId == subjectId))
                                .Select(s => new BulkAttendanceStudentViewModel {
                                    StudentId = s.Id,
                                    EnrollmentNo = s.EnrollmentNo,
                                    StudentName = s.Name
                                })
                                .ToListAsync();

            if (!students.Any())
            {
                return Json(new { success = false, message = "No students found mapped to this subject." });
            }

            var records = await _context.AttendanceRecords
                                .Where(a => a.SubjectId == subjectId &&
                                        a.Date >= startDate &&
                                        a.Date <= endDate)
                                .ToListAsync();

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
        public async Task<IActionResult> SaveBulkAttendance([FromBody] ViewModels.BulkAttendanceSaveModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data submitted" });
                }

                DateTime startDate, endDate;

                // Handle both monthly and date range scenarios
                if (model.Month > 0 && model.Year > 0)
                {
                    startDate = new DateTime(model.Year, model.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                else
                {
                    var firstDateKey = model.AttendanceData.FirstOrDefault().Value?.Keys.FirstOrDefault();
                    if (string.IsNullOrEmpty(firstDateKey))
                    {
                        return Json(new { success = false, message = "No date data found" });
                    }
                    startDate = DateTime.Parse(firstDateKey);
                    endDate = DateTime.Parse(model.AttendanceData.FirstOrDefault().Value.Keys.Last());
                }

                // Get existing records for comparison and auditing
                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == model.SubjectId &&
                               a.Date >= startDate &&
                               a.Date <= endDate)
                    .ToDictionaryAsync(r => (r.StudentId, r.Date.Date), r => r);

                // Track changes and create audit records
                var auditRecords = new List<AttendanceAudit>();
                var updatedRecords = new List<AttendanceRecord>();
                var newRecords = new List<AttendanceRecord>();

                foreach (var studentEntry in model.AttendanceData)
                {
                    var studentId = int.Parse(studentEntry.Key);
                    foreach (var dateEntry in studentEntry.Value)
                    {
                        var date = DateTime.Parse(dateEntry.Key);
                        if (!dateEntry.Value.HasValue) continue; // Skip if not marked

                        var key = (studentId, date.Date);
                        if (existingRecords.TryGetValue(key, out var existingRecord))
                        {
                            // Update existing record if value changed
                            if (existingRecord.IsPresent != dateEntry.Value.Value)
                            {
                                // Create audit record
                                auditRecords.Add(new AttendanceAudit
                                {
                                    AttendanceRecordId = existingRecord.Id,
                                    ModifiedById = CurrentUser.Id,
                                    ModifiedOn = DateTime.Now,
                                    OldValue = existingRecord.IsPresent,
                                    NewValue = dateEntry.Value.Value
                                });

                                // Update existing record
                                existingRecord.IsPresent = dateEntry.Value.Value;
                                existingRecord.TimeStamp = DateTime.Now;
                                updatedRecords.Add(existingRecord);
                            }
                        }
                        else
                        {
                            // Create new record
                            var newRecord = new AttendanceRecord
                            {
                                StudentId = studentId,
                                SubjectId = model.SubjectId,
                                Date = date,
                                IsPresent = dateEntry.Value.Value,
                                TimeStamp = DateTime.Now,
                                MarkedById = CurrentUser.Id
                            };
                            newRecords.Add(newRecord);
                        }
                    }
                }

                // Save changes to database
                if (auditRecords.Any())
                    _context.AttendanceAudits.AddRange(auditRecords);
                if (updatedRecords.Any())
                    _context.AttendanceRecords.UpdateRange(updatedRecords);
                if (newRecords.Any())
                    _context.AttendanceRecords.AddRange(newRecords);

                await _context.SaveChangesAsync();
                return Json(new { 
                    success = true, 
                    message = "Attendance saved successfully",
                    updated = updatedRecords.Count,
                    new_records = newRecords.Count,
                    audits = auditRecords.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bulk attendance");
                return Json(new { success = false, message = "Failed to save attendance: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TopicDiscussions()
        {
            var model = new TopicDiscussionsReportViewModel
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };

            if (User.IsInRole("Admin"))
            {
                model.Teachers = new SelectList(
                    await _context.Users.Where(u => u.Role == "Teacher").ToListAsync(),
                    "Id", "UserName");
                model.Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name");
            }
            else
            {
                model.TeacherId = CurrentUser.Id;
                var teacherSubjects = await _context.TeacherSubjects
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.UserId == CurrentUser.Id && ts.IsActive)
                    .Select(ts => ts.Subject)
                    .ToListAsync();
                model.Subjects = new SelectList(teacherSubjects, "Id", "Name");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopicDiscussionsReport(int? teacherId, int subjectId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = from ar in _context.AttendanceRecords
                            join s in _context.Subjects on ar.SubjectId equals s.Id
                            join u in _context.Users on ar.MarkedById equals u.Id
                            where ar.SubjectId == subjectId &&
                                  ar.Date.Date >= startDate.Date &&
                                  ar.Date.Date <= endDate.Date
                            group ar by new { ar.Date, ar.SubjectId, ar.MarkedById, u.UserName, ar.DiscussionTopic, s.Name } into g
                            select new TopicDiscussionItem
                            {
                                Date = g.Key.Date,
                                SubjectName = g.Key.Name,
                                MarkedById = g.Key.MarkedById,
                                TeacherName = g.Key.UserName,
                                DiscussionTopic = g.Key.DiscussionTopic ?? "-",
                                StudentsPresent = g.Count(x => x.IsPresent),
                                TotalStudents = g.Count()
                            };

                if (teacherId.HasValue)
                {
                    query = query.Where(x => x.MarkedById == teacherId.Value);
                }

                var discussions = await query
                    .OrderByDescending(d => d.Date)
                    .ToListAsync();

                return PartialView("_TopicDiscussionsReport", discussions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating topic discussions report");
                return Json(new { success = false, message = "Error generating report" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MonthlyReport()
        {
            var model = new MonthlyReportViewModel { ReportDate = DateTime.Today };
            
            if (User.IsInRole("Admin"))
            {
                model.Teachers = new SelectList(
                    await _context.Users.Where(u => u.Role == "Teacher").ToListAsync(),
                    "Id", "UserName");
            }
            else
            {
                model.TeacherId = CurrentUser.Id;
                var subjects = await _context.TeacherSubjects
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.UserId == CurrentUser.Id && ts.IsActive)
                    .Select(ts => ts.Subject)
                    .ToListAsync();
                model.Subjects = new SelectList(subjects, "Id", "Name");
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyReport(int teacherId, int subjectId, string reportMonth, bool skipEmptyDates)
        {
            try
            {
                var date = DateTime.ParseExact(reportMonth, "yyyy-MM", CultureInfo.InvariantCulture);
                var institute = await _context.Institutes.FirstOrDefaultAsync();
                
                // Get subject details with class and course
                var subject = await _context.Subjects
                    .Include(s => s.Class)
                    .ThenInclude(c => c.Course)
                    .FirstOrDefaultAsync(s => s.Id == subjectId);
                    
                if (subject == null)
                    return Json(new { success = false, message = "Subject not found" });
                    
                // Get teacher name
                var teacher = await _context.Users.FindAsync(teacherId);
                if (teacher == null)
                    return Json(new { success = false, message = "Teacher not found" });
                    
                // Get academic year
                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.IsActive);
                    
                // Get all dates in the month excluding Sundays
                var datesInMonth = Enumerable.Range(1, DateTime.DaysInMonth(date.Year, date.Month))
                    .Select(day => new DateTime(date.Year, date.Month, day))
                    .Where(d => d.DayOfWeek != DayOfWeek.Sunday)
                    .ToList();
                    
                // Get attendance records
                var records = await _context.AttendanceRecords
                    .Where(ar => ar.SubjectId == subjectId &&
                                ar.Date.Month == date.Month &&
                                ar.Date.Year == date.Year)
                    .ToListAsync();

                // Filter dates if skipEmptyDates is true
                var finalDates = skipEmptyDates 
                    ? datesInMonth.Where(d => records.Any(r => r.Date.Date == d.Date)).ToList()
                    : datesInMonth;

                // Get active students who are enrolled in this subject
                var students = await _context.Students
                    .Include(s => s.StudentSubjects)
                    .Where(s => s.ClassId == subject.ClassId && 
                               s.IsActive &&
                               s.StudentSubjects.Any(ss => ss.SubjectId == subjectId))
                    .OrderBy(s => s.EnrollmentNo)
                    .ToListAsync();

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No active students found for this subject." });
                }

                // Get division name (using first student's division as they're all in same class)
                var division = await _context.Divisions
                    .FirstOrDefaultAsync(d => d.Id == students.First().DivisionId);
                    
                // Get course and specialization details
                var classDetails = await _context.Classes
                    .Include(c => c.Course)
                    .Include(c => c.Specialization)
                    .FirstOrDefaultAsync(c => c.Id == subject.ClassId);

                // Get the current website URL
                var request = HttpContext.Request;
                var currentUrl = $"{request.Scheme}://{request.Host}";

                // Prepare report data
                var reportData = new MonthlyAttendanceReportData
                {
                    InstituteName = institute?.Name ?? "Institute Name",
                    InstituteAddress = institute?.Address,
                    TeacherName = teacher.UserName,
                    SubjectInfo = $"{subject.Code} - {subject.Name}",
                    ClassInfo = classDetails?.Name ?? "N/A",
                    CourseName = classDetails?.Course?.Name,
                    Specialization = classDetails?.Specialization?.Name,
                    DivisionName = division?.Name ?? "All Divisions",
                    AcademicYear = academicYear?.Name ?? "Current Academic Year",
                    MonthYear = date.ToString("MMMM yyyy"),
                    WebsiteUrl = currentUrl, // Set current website URL instead of institute.Website
                    Dates = finalDates,
                    Students = students.Select(s => new StudentMonthlyAttendance
                    {
                        EnrollmentNo = s.EnrollmentNo,
                        StudentName = s.Name,
                        AttendanceByDate = finalDates.ToDictionary(
                            d => d,
                            d => (bool?)records.FirstOrDefault(r => 
                                r.StudentId == s.Id && r.Date.Date == d.Date)?.IsPresent
                        )
                    }).ToList(),
                    TeacherAttribution = records
                        .GroupBy(r => r.MarkedBy.UserName)
                        .Select(g => $"{g.Key}: {g.Count()} records")
                        .ToList()
                };

                return PartialView("_MonthlyReportView", reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                return Json(new { success = false, message = "Error generating report" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportMonthlyReport(int teacherId, int subjectId, string reportMonth, bool skipEmptyDates)
        {
            try
            {
                var reportResult = await GetMonthlyReport(teacherId, subjectId, reportMonth, skipEmptyDates) as PartialViewResult;
                if (reportResult?.Model is not MonthlyAttendanceReportData reportData)
                {
                    return BadRequest("Failed to generate report data");
                }

                var pdfBytes = GeneratePdf(reportData);
                return File(pdfBytes, "application/pdf", $"attendance-report-{reportMonth}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly report");
                return BadRequest("Error exporting report");
            }
        }

        private byte[] GeneratePdf(MonthlyAttendanceReportData reportData)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4.Rotate());

                // Set smaller margins
                document.SetMargins(15, 10, 25, 10);  // top, right, bottom, left

                // Add page numbers
                pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new PageNumberEventHandler());

                // Header - Only on first page
                var headerTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(5);
                headerTable.AddCell(CreateCell(reportData.InstituteName)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetBorder(new SolidBorder(0))
                    .SetPadding(2));

                if (!string.IsNullOrEmpty(reportData.InstituteAddress))
                {
                    headerTable.AddCell(CreateCell(reportData.InstituteAddress)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetBorder(new SolidBorder(0))
                        .SetPadding(2));
                }
                document.Add(headerTable);

                // Report Title
                document.Add(new Paragraph("Monthly Attendance Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(12)
                    .SetMarginBottom(5));

                // Info Table - Only on first page
                var infoTable = new Table(3).UseAllAvailableWidth().SetMarginBottom(5);
                AddCompactInfoRow(infoTable, "Teacher", reportData.TeacherName);
                AddCompactInfoRow(infoTable, "Subject", reportData.SubjectInfo);
                AddCompactInfoRow(infoTable, "Month", reportData.MonthYear);
                
                if (!string.IsNullOrEmpty(reportData.CourseName))
                    AddCompactInfoRow(infoTable, "Course", reportData.CourseName);
                    
                AddCompactInfoRow(infoTable, "Class", reportData.ClassInfo);
                
                if (!string.IsNullOrEmpty(reportData.Specialization))
                    AddCompactInfoRow(infoTable, "Specialization", reportData.Specialization);
                    
                AddCompactInfoRow(infoTable, "Division", reportData.DivisionName);
                AddCompactInfoRow(infoTable, "Academic Year", reportData.AcademicYear);
                document.Add(infoTable);

                // Optimize column widths
                float[] columnWidths = new float[reportData.Dates.Count + 2];
                columnWidths[0] = 120f; // Student details
                for (int i = 1; i < columnWidths.Length - 1; i++)
                    columnWidths[i] = 25f; // Date columns
                columnWidths[columnWidths.Length - 1] = 25f; // Percentage column - reduced

                // Split students into groups of 40 per page (after first page)
                var firstPageCount = 30; // Fewer students on first page due to header
                var remainingPagesCount = 40; // More students on subsequent pages

                var firstPageStudents = reportData.Students.Take(firstPageCount).ToList();
                var remainingStudents = reportData.Students.Skip(firstPageCount)
                    .Select((x, i) => new { Student = x, Index = i })
                    .GroupBy(x => x.Index / remainingPagesCount)
                    .Select(g => g.Select(x => x.Student).ToList())
                    .ToList();

                // First page
                var table = new Table(UnitValue.CreatePointArray(columnWidths))
                    .UseAllAvailableWidth()
                    .SetMarginTop(5);
                AddTableHeader(table, reportData.Dates);
                foreach (var student in firstPageStudents)
                {
                    AddStudentRow(table, student, reportData.Dates);
                }
                document.Add(table);

                // Remaining pages
                foreach (var studentGroup in remainingStudents)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    
                    table = new Table(UnitValue.CreatePointArray(columnWidths))
                        .UseAllAvailableWidth()
                        .SetMarginTop(5);
                    AddTableHeader(table, reportData.Dates);
                    
                    foreach (var student in studentGroup)
                    {
                        AddStudentRow(table, student, reportData.Dates);
                    }
                    document.Add(table);
                }

                // Update footer with both date and website URL
                var footerTable = new Table(1).UseAllAvailableWidth().SetMarginTop(20);
                footerTable.AddCell(CreateCell(
                    $"Report Generated on {DateTime.Now:dd/MM/yyyy HH:mm} | {reportData.WebsiteUrl}")
                    .SetBorder(new SolidBorder(0))
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER));
                document.Add(footerTable);

                document.Close();
                return ms.ToArray();
            }
        }

        // New helper methods
        private void AddTableHeader(Table table, List<DateTime> dates)
        {
            table.AddCell(CreateCompactHeaderCell("Student Details"));
            foreach (var date in dates)
            {
                table.AddCell(CreateCompactHeaderCell(date.Day.ToString()));
            }
            table.AddCell(CreateCompactHeaderCell("%"));
        }

        private void AddStudentRow(Table table, StudentMonthlyAttendance student, List<DateTime> dates)
        {
            // Student Info - more compact
            table.AddCell(CreateCompactCell($"{student.EnrollmentNo}\n{student.StudentName}"));

            // Attendance Cells
            int presentCount = 0;
            int totalDays = 0;
            
            foreach (var date in dates)
            {
                var status = student.AttendanceByDate.GetValueOrDefault(date);
                if (status.HasValue)
                {
                    totalDays++;
                    if (status.Value) presentCount++;
                }
                table.AddCell(CreateCompactCell(status.HasValue ? (status.Value ? "P" : "A") : "-"));
            }

            var percentage = totalDays > 0 ? (presentCount * 100.0 / totalDays) : 0;
            table.AddCell(CreateCompactCell($"{percentage:F1}%"));
        }

        private Cell CreateCompactHeaderCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFontSize(8))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(3)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
        }

        private Cell CreateCompactCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFontSize(8))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(3)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
        }

        private void AddCompactInfoRow(Table table, string label, string value)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph($"{label}: {value}")
                    .SetFontSize(8))
                .SetBorder(new SolidBorder(0))
                .SetPadding(2));
        }

        // Page number handler class
        private class PageNumberEventHandler : IEventHandler 
        {
            public void HandleEvent(Event currentEvent)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
                PdfPage page = docEvent.GetPage();
                int pageNumber = docEvent.GetDocument().GetPageNumber(page);

                Rectangle pageSize = page.GetPageSize();
                PdfCanvas canvas = new PdfCanvas(page);
                
                canvas.BeginText()
                    .SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText(pageSize.GetWidth() / 2 - 20, 30)
                    .ShowText($"Page {pageNumber}")
                    .EndText();
            }
        }

        private Cell CreateCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);
        }

        [HttpGet]
        public async Task<IActionResult> ClassWiseReport()
        {
            var model = new ClassWiseReportViewModel
            {
                AcademicYears = new SelectList(await _context.AcademicYears
                    .Where(ay => ay.IsActive)
                    .ToListAsync(), "Id", "Name"),
                Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name")
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses(int courseId, int academicYearId)
        {
            var classes = await _context.Classes
                .Where(c => c.CourseId == courseId && c.AcademicYearId == academicYearId)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();
            return Json(classes);
        }

        [HttpGet]
        public async Task<IActionResult> GetClassWiseReport(int academicYearId, int courseId, int classId)
        {
            try
            {
                var institute = await _context.Institutes.FirstOrDefaultAsync();
                var classInfo = await _context.Classes
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.Id == classId);

                if (classInfo == null)
                    return Json(new { success = false, message = "Class not found" });

                var students = await _context.Students
                    .Include(s => s.Division)
                    .Include(s => s.Class)
                    .ThenInclude(c => c.Specialization)
                    .Where(s => s.ClassId == classId && s.IsActive)
                    .OrderBy(s => s.EnrollmentNo)
                    .ToListAsync();

                var subjects = await _context.Subjects
                    .Where(s => s.ClassId == classId)
                    .ToListAsync();

                var attendanceRecords = await _context.AttendanceRecords
                    .Where(ar => ar.Student.ClassId == classId)
                    .ToListAsync();

                var reportData = new ClassWiseReportData
                {
                    InstituteName = institute?.Name ?? "Institute Name",
                    InstituteAddress = institute?.Address,
                    CourseName = classInfo.Course.Name,
                    ClassName = classInfo.Name,
                    AcademicYear = (await _context.AcademicYears.FindAsync(academicYearId))?.Name ?? "Current Year",
                    WebsiteUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}",
                    Students = students.Select(s => new StudentAttendanceSummary
                    {
                        EnrollmentNo = s.EnrollmentNo,
                        StudentName = s.Name,
                        Division = s.Division?.Name ?? "N/A",
                        Specialization = s.Class?.Specialization?.Name ?? "N/A",
                        SubjectAttendances = subjects.Select(subj => {
                            var records = attendanceRecords.Where(ar => 
                                ar.StudentId == s.Id && ar.SubjectId == subj.Id);
                            var totalClasses = records.Count();
                            var present = records.Count(r => r.IsPresent);
                            return new SubjectAttendance
                            {
                                SubjectName = subj.Name,
                                TotalClasses = totalClasses,
                                Present = present,
                                Percentage = totalClasses > 0 ? (present * 100.0M / totalClasses) : 0
                            };
                        }).ToList()
                    }).ToList()
                };

                // Calculate overall percentage for each student
                foreach (var student in reportData.Students)
                {
                    var totalClasses = student.SubjectAttendances.Sum(sa => sa.TotalClasses);
                    var totalPresent = student.SubjectAttendances.Sum(sa => sa.Present);
                    student.OverallPercentage = totalClasses > 0 ? (totalPresent * 100.0M / totalClasses) : 0;
                }

                return PartialView("_ClassWiseReportView", reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating class wise report");
                return Json(new { success = false, message = "Error generating report" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportClassWiseReport(int academicYearId, int courseId, int classId)
        {
            try
            {
                var reportResult = await GetClassWiseReport(academicYearId, courseId, classId) as PartialViewResult;
                if (reportResult?.Model is not ClassWiseReportData reportData)
                {
                    return BadRequest("Failed to generate report data");
                }

                var pdfBytes = GenerateClassWisePdf(reportData);
                return File(pdfBytes, "application/pdf", $"class-wise-report-{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting class wise report");
                return BadRequest("Error exporting report");
            }
        }

        private byte[] GenerateClassWisePdf(ClassWiseReportData reportData)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4);

                // Set margins (top, right, bottom, left)
                document.SetMargins(15, 15, 25, 15);

                // Add page numbers
                pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new PageNumberEventHandler());

                // Header
                var headerTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(10);
                headerTable.AddCell(CreateCell(reportData.InstituteName)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(2));

                if (!string.IsNullOrEmpty(reportData.InstituteAddress))
                {
                    headerTable.AddCell(CreateCell(reportData.InstituteAddress)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetBorder(Border.NO_BORDER)
                        .SetPadding(2));
                }
                document.Add(headerTable);

                // Report Title
                document.Add(new Paragraph("Class Wise Attendance Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(12)
                    .SetMarginBottom(10));

                // Report Info
                var infoTable = new Table(2).UseAllAvailableWidth().SetMarginBottom(10);
                AddInfoRow(infoTable, "Course", reportData.CourseName);
                AddInfoRow(infoTable, "Class", reportData.ClassName);
                AddInfoRow(infoTable, "Academic Year", reportData.AcademicYear);
                document.Add(infoTable);

                // Check if specialization column should be shown
                bool showSpecialization = reportData.Students.Any(s => 
                    !string.IsNullOrEmpty(s.Specialization) && s.Specialization != "N/A");

                // Calculate column widths based on subjects and specialization
                var subjectCount = reportData.Students.FirstOrDefault()?.SubjectAttendances.Count ?? 0;
                var columnCount = 4 + subjectCount + (showSpecialization ? 1 : 0); // Basic columns + subjects + specialization (if shown) + overall
                var columnWidths = new float[columnCount];
                
                // Set column widths
                int currentColumn = 0;
                columnWidths[currentColumn++] = 80; // Enrollment
                columnWidths[currentColumn++] = 120; // Name
                columnWidths[currentColumn++] = 60; // Division
                if (showSpecialization)
                {
                    columnWidths[currentColumn++] = 80; // Specialization
                }
                // Set subject column widths
                for (int i = currentColumn; i < columnWidths.Length - 1; i++)
                {
                    columnWidths[i] = 50; // Subject columns
                }
                columnWidths[columnWidths.Length - 1] = 50; // Overall column

                // Create attendance table
                var table = new Table(UnitValue.CreatePointArray(columnWidths))
                    .UseAllAvailableWidth()
                    .SetFontSize(8);

                // Add header row
                table.AddHeaderCell(CreateHeaderCell("Enrollment No"));
                table.AddHeaderCell(CreateHeaderCell("Student Name"));
                table.AddHeaderCell(CreateHeaderCell("Division"));
                if (showSpecialization)
                {
                    table.AddHeaderCell(CreateHeaderCell("Specialization"));
                }
                foreach (var subject in reportData.Students.FirstOrDefault()?.SubjectAttendances ?? new List<SubjectAttendance>())
                {
                    table.AddHeaderCell(CreateHeaderCell(subject.SubjectName));
                }
                table.AddHeaderCell(CreateHeaderCell("Overall %"));

                // Add data rows
                foreach (var student in reportData.Students)
                {
                    table.AddCell(CreateCell(student.EnrollmentNo));
                    table.AddCell(CreateCell(student.StudentName));
                    table.AddCell(CreateCell(student.Division));
                    if (showSpecialization)
                    {
                        table.AddCell(CreateCell(student.Specialization));
                    }
                    foreach (var subject in student.SubjectAttendances)
                    {
                        table.AddCell(CreateCell($"{subject.Percentage:F1}%\n({subject.Present}/{subject.TotalClasses})"));
                    }
                    table.AddCell(CreateCell($"{student.OverallPercentage:F1}%")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
                }

                // Add the table to the document! (This was missing)
                document.Add(table);

                // Footer with generation date and website
                var footerTable = new Table(1).UseAllAvailableWidth().SetMarginTop(20);
                footerTable.AddCell(CreateCell(
                    $"Report Generated on {DateTime.Now:dd/MM/yyyy HH:mm} | {reportData.WebsiteUrl}")
                    .SetBorder(Border.NO_BORDER)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER));
                document.Add(footerTable);

                document.Close();
                return ms.ToArray();
            }
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            table.AddCell(CreateCell(label + ":")
                .SetBorder(Border.NO_BORDER)
                .SetFontSize(9)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
            table.AddCell(CreateCell(value)
                .SetBorder(Border.NO_BORDER)
                .SetFontSize(9));
        }

        private Cell CreateHeaderCell(string text)
        {
            return CreateCell(text)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
        }
    }
}
