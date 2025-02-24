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

                // Remove existing records for this date and subject
                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.Date.Date == model.Date.Date && 
                               a.SubjectId == model.SubjectId)
                    .ToListAsync();

                _context.AttendanceRecords.RemoveRange(existingRecords);

                // Add new records
                foreach (var student in model.Students)
                {
                    var attendance = new AttendanceRecord
                    {
                        StudentId = student.StudentId,
                        SubjectId = model.SubjectId,
                        Date = model.Date,
                        IsPresent = student.IsPresent,
                        TimeStamp = DateTime.Now,
                        MarkedById = User.Identity?.Name ?? "Unknown",
                        DiscussionTopic = model.DiscussionTopic // Make sure this is properly set
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

                // Remove only existing records for dates that are being submitted
                var datesBeingSubmitted = new HashSet<DateTime>();
                foreach (var studentEntry in model.AttendanceData)
                {
                    foreach (var dateEntry in studentEntry.Value)
                    {
                        datesBeingSubmitted.Add(DateTime.Parse(dateEntry.Key).Date);
                    }
                }

                var existingRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == model.SubjectId &&
                               a.Date >= startDate &&
                               a.Date <= endDate &&
                               datesBeingSubmitted.Contains(a.Date.Date))
                    .ToListAsync();

                _context.AttendanceRecords.RemoveRange(existingRecords);

                // Add new records only for dates where attendance was marked
                foreach (var studentEntry in model.AttendanceData)
                {
                    var studentId = int.Parse(studentEntry.Key);
                    foreach (var dateEntry in studentEntry.Value)
                    {
                        var date = DateTime.Parse(dateEntry.Key);
                        // Only create record if explicitly marked (checkbox was clicked)
                        if (dateEntry.Value.HasValue) // Check for HasValue since it's nullable
                        {
                            var attendance = new AttendanceRecord
                            {
                                StudentId = studentId,
                                SubjectId = model.SubjectId,
                                Date = date,
                                IsPresent = dateEntry.Value.Value, // Use .Value to get the bool value
                                TimeStamp = DateTime.Now,
                                MarkedById = User.Identity?.Name ?? "System"
                            };
                            _context.AttendanceRecords.Add(attendance);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attendance saved successfully" });
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
                            where ar.SubjectId == subjectId &&
                                  ar.Date.Date >= startDate.Date &&
                                  ar.Date.Date <= endDate.Date
                            group ar by new { ar.Date, ar.SubjectId, ar.MarkedById, ar.DiscussionTopic, s.Name } into g
                            select new TopicDiscussionItem
                            {
                                Date = g.Key.Date,
                                SubjectName = g.Key.Name,
                                TeacherName = g.Key.MarkedById,
                                DiscussionTopic = g.Key.DiscussionTopic ?? "-",
                                StudentsPresent = g.Count(x => x.IsPresent),
                                TotalStudents = g.Count()
                            };

                if (teacherId.HasValue)
                {
                    var teacherUsername = await _context.Users
                        .Where(u => u.Id == teacherId)
                        .Select(u => u.UserName)
                        .FirstOrDefaultAsync();
                    query = query.Where(x => x.TeacherName == teacherUsername);
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
                    
                // Prepare report data
                var reportData = new MonthlyAttendanceReportData
                {
                    InstituteName = institute?.Name ?? "Institute Name",
                    TeacherName = teacher.UserName,
                    SubjectInfo = $"{subject.Code} - {subject.Name}",
                    ClassInfo = $"{subject.Class.Course.Name} - {subject.Class.Name}",
                    DivisionName = division?.Name ?? "All Divisions",
                    AcademicYear = academicYear?.Name ?? "Current Academic Year",
                    MonthYear = date.ToString("MMMM yyyy"),
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
                    }).ToList()
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
                // Re-use the GetMonthlyReport logic to get report data
                var report = await GetMonthlyReport(teacherId, subjectId, reportMonth, skipEmptyDates) as PartialViewResult;
                if (report?.Model == null)
                    return BadRequest("Failed to generate report");

                var reportData = report.Model as MonthlyAttendanceReportData;
                
                // Generate PDF using a PDF library like iTextSharp or similar
                // This is a placeholder for the actual PDF generation logic
                var pdfBytes = GeneratePdf(reportData);
                
                return File(pdfBytes, "application/pdf", 
                    $"attendance-report-{reportMonth}.pdf");
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
                using (var writer = new PdfWriter(ms))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        var document = new Document(pdf, PageSize.A4.Rotate());  // Landscape mode
                        document.SetMargins(20, 20, 20, 20);

                        // Add institute header
                        var header = new Paragraph(reportData.InstituteName)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(16)
                            .SetBold();
                        document.Add(header);

                        // Add report info
                        var info = new Table(3)
                            .UseAllAvailableWidth()
                            .SetMarginTop(10);

                        info.AddCell(new Cell().Add(new Paragraph($"Teacher: {reportData.TeacherName}")));
                        info.AddCell(new Cell().Add(new Paragraph($"Subject: {reportData.SubjectInfo}")));
                        info.AddCell(new Cell().Add(new Paragraph($"Month: {reportData.MonthYear}")));
                        info.AddCell(new Cell().Add(new Paragraph($"Class: {reportData.ClassInfo}")));
                        info.AddCell(new Cell().Add(new Paragraph($"Division: {reportData.DivisionName}")));
                        info.AddCell(new Cell().Add(new Paragraph($"Academic Year: {reportData.AcademicYear}")));

                        document.Add(info);

                        // Create attendance table
                        var table = new Table(reportData.Dates.Count + 2)  // +2 for student details and percentage
                            .UseAllAvailableWidth()
                            .SetMarginTop(20);

                        // Add header row
                        table.AddCell(new Cell().Add(new Paragraph("Student Details")).SetBold());
                        foreach (var date in reportData.Dates)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(date.Day.ToString())).SetBold().SetTextAlignment(TextAlignment.CENTER));
                        }
                        table.AddCell(new Cell().Add(new Paragraph("%")).SetBold().SetTextAlignment(TextAlignment.CENTER));

                        // Add student rows
                        foreach (var student in reportData.Students)
                        {
                            table.AddCell(new Cell().Add(new Paragraph($"{student.EnrollmentNo}\n{student.StudentName}")));
                            
                            foreach (var date in reportData.Dates)
                            {
                                var status = student.AttendanceByDate.GetValueOrDefault(date);
                                var text = status.HasValue ? (status.Value ? "P" : "A") : "-";
                                var cell = new Cell().Add(new Paragraph(text)).SetTextAlignment(TextAlignment.CENTER);
                                table.AddCell(cell);
                            }

                            // Calculate percentage
                            var totalDays = student.AttendanceByDate.Count(x => x.Value.HasValue);
                            var presentDays = student.AttendanceByDate.Count(x => x.Value == true);
                            var percentage = totalDays > 0 ? (presentDays * 100.0 / totalDays) : 0;
                            table.AddCell(new Cell().Add(new Paragraph($"{percentage:F1}%")).SetTextAlignment(TextAlignment.CENTER));
                        }

                        document.Add(table);

                        // Add footer
                        var footer = new Paragraph($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(20);
                        document.Add(footer);

                        document.Close();
                    }
                }
                return ms.ToArray();
            }
        }
    }
}
