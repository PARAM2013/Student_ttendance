using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;

namespace Student_Attendance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentCarryForwardController : BaseController
    {
        private readonly ILogger<StudentCarryForwardController> _logger;

        public StudentCarryForwardController(ApplicationDbContext context, ILogger<StudentCarryForwardController> logger) 
            : base(context)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new StudentCarryForwardViewModel();
            await LoadDropdowns(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPreviewData(int currentYearId, int nextYearId, int courseId, int? classId, int? divisionId, int nextCourseId, int? nextClassId)
        {
            try
            {
                var query = _context.Students
                    .Include(s => s.Class)
                    .Include(s => s.Division)
                    .Where(s => s.AcademicYearId == currentYearId && 
                               s.CourseId == courseId && 
                               s.IsActive);

                if (classId.HasValue)
                    query = query.Where(s => s.ClassId == classId.Value);
                if (divisionId.HasValue)
                    query = query.Where(s => s.DivisionId == divisionId.Value);

                var students = await query.ToListAsync();

                var nextClass = nextClassId.HasValue 
                    ? await _context.Classes.FirstOrDefaultAsync(c => c.Id == nextClassId.Value)
                    : null;

                if (nextClassId.HasValue && nextClass == null)
                    return Json(new { success = false, message = "Next class configuration not found" });

                var promotionData = students.Select(s => new StudentPromotionData
                {
                    StudentId = s.Id,
                    EnrollmentNo = s.EnrollmentNo,
                    Name = s.Name,
                    CurrentSemester = s.Semester,
                    NextSemester = s.Semester + 1,
                    CurrentClass = s.Class?.Name ?? "N/A",
                    NextClass = nextClass?.Name ?? "Not Set",
                    CurrentDivision = s.Division?.Name ?? "N/A",
                    Selected = true
                }).ToList();

                return PartialView("_PreviewData", promotionData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview data");
                return Json(new { success = false, message = "Error loading preview data" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MoveData([FromBody] StudentCarryForwardViewModel model)
        {
            try
            {
                _logger.LogInformation("Received request for MoveData");
                string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                _logger.LogInformation($"Raw request body: {requestBody}");
                
                if (model == null)
                {
                    _logger.LogError("Model is null");
                    return Json(new { success = false, message = "Invalid request data", details = "Model is null" });
                }

                if (model.StudentsToPromote == null || !model.StudentsToPromote.Any())
                {
                    _logger.LogError("StudentsToPromote is null or empty");
                    return Json(new { success = false, message = "No students selected" });
                }

                _logger.LogInformation($"Processing {model.StudentsToPromote.Count} students");
                
                var debugInfo = new
                {
                    HasModel = model != null,
                    StudentsCount = model?.StudentsToPromote?.Count ?? 0,
                    CurrentYear = model?.CurrentAcademicYearId,
                    NextYear = model?.NextAcademicYearId,
                    NextClass = model?.NextClassId
                };
                _logger.LogInformation("Debug Info: {@DebugInfo}", debugInfo);

                if (model.StudentsToPromote == null)
                {
                    _logger.LogError("StudentsToPromote is null");
                    return Json(new { success = false, message = "No students data received" });
                }

                if (!model.StudentsToPromote.Any())
                {
                    _logger.LogError("StudentsToPromote is empty");
                    return Json(new { success = false, message = "No students selected" });
                }

                if (!model.NextClassId.HasValue)
                {
                    _logger.LogError("NextClassId is null");
                    return Json(new { success = false, message = "Next class must be selected" });
                }

                // Log the data being sent
                _logger.LogInformation($"Moving {model.StudentsToPromote.Count} students");
                _logger.LogInformation($"From Year: {model.CurrentAcademicYearId} to Year: {model.NextAcademicYearId}");
                _logger.LogInformation($"From Course: {model.CourseId} to Course: {model.NextCourseId}");
                _logger.LogInformation($"To Class: {model.NextClassId} Division: {model.NextDivisionId}");

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var userName = User.Identity?.Name ?? "System";
                    var processedCount = 0;

                    foreach (var studentData in model.StudentsToPromote.Where(s => s?.Selected == true))
                    {
                        var student = await _context.Students
                            .Include(s => s.Class)
                            .Include(s => s.Division)
                            .FirstOrDefaultAsync(s => s.Id == studentData.StudentId && s.IsActive);

                        if (student != null)
                        {
                            // Create enrollment history record
                            var history = new StudentEnrollmentHistory
                            {
                                StudentId = student.Id,
                                EnrollmentNo = student.EnrollmentNo ?? "",
                                AcademicYearId = student.AcademicYearId,
                                CourseId = student.CourseId,
                                ClassId = student.ClassId,
                                DivisionId = student.DivisionId,
                                Semester = student.Semester,
                                CreatedBy = userName,
                                IsActive = false
                            };
                            await _context.StudentEnrollmentHistories.AddAsync(history);

                            // Archive attendance records
                            await ArchiveAttendanceRecords(student.Id, student.AcademicYearId, userName);

                            // Update student record
                            student.AcademicYearId = model.NextAcademicYearId;
                            student.CourseId = model.NextCourseId;
                            student.ClassId = model.NextClassId.Value;
                            student.DivisionId = model.NextDivisionId;
                            student.Semester = studentData.NextSemester;

                            _context.Students.Update(student);
                            processedCount++;
                        }
                    }

                    if (processedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Json(new { 
                            success = true, 
                            message = $"Successfully processed {processedCount} students",
                            processedCount = processedCount 
                        });
                    }

                    throw new Exception("No students were processed");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in student carry forward");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}", 
                    details = ex.StackTrace 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PublishData([FromBody] StudentCarryForwardViewModel model)
        {
            try
            {
                if (!model.StudentsToPromote.Any())
                    return Json(new { success = false, message = "No students selected for promotion" });

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var student in model.StudentsToPromote.Where(s => s.Selected))
                    {
                        var existingStudent = await _context.Students.FindAsync(student.StudentId);
                        if (existingStudent != null)
                        {
                            // Map student to new subjects based on next class and semester
                            var nextClass = await _context.Classes
                                .Include(c => c.Subjects)
                                .FirstOrDefaultAsync(c => c.Name == student.NextClass && 
                                                        c.CourseId == existingStudent.CourseId);

                            if (nextClass != null)
                            {
                                // Remove old subject mappings
                                var oldMappings = await _context.StudentSubjects
                                    .Where(ss => ss.StudentId == student.StudentId)
                                    .ToListAsync();
                                _context.StudentSubjects.RemoveRange(oldMappings);

                                // Add new subject mappings
                                var newSubjects = nextClass.Subjects
                                    .Where(s => s.Semester == student.NextSemester)
                                    .Select(s => new StudentSubject
                                    {
                                        StudentId = student.StudentId,
                                        SubjectId = s.Id
                                    });

                                await _context.StudentSubjects.AddRangeAsync(newSubjects);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Data published successfully" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing data");
                return Json(new { success = false, message = "Error publishing data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClassesByCourse(int courseId, int academicYearId)
        {
            try
            {
                var classes = await _context.Classes
                    .Where(c => c.CourseId == courseId && c.AcademicYearId == academicYearId)
                    .Select(c => new { id = c.Id, name = c.Name })
                    .ToListAsync();
                return Json(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting classes by course");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDivisionsByClass(int classId)
        {
            try
            {
                var divisions = await _context.Divisions
                    .Where(d => d.ClassId == classId)
                    .Select(d => new { id = d.Id, name = d.Name })
                    .ToListAsync();

                _logger.LogInformation($"Found {divisions.Count} divisions for class {classId}");
                return Json(divisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting divisions by class");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByYear(int yearId)
        {
            try
            {
                var courses = await _context.Courses
                    .Select(c => new { id = c.Id, name = c.Name })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses by year");
                return Json(new List<object>());
            }
        }

        private async Task LoadDropdowns(StudentCarryForwardViewModel model)
        {
            var years = await _context.AcademicYears
                .OrderByDescending(y => y.StartDate)
                .ToListAsync();

            var courses = await _context.Courses.ToListAsync();

            model.AcademicYears = new SelectList(years, "Id", "Name");
            model.NextAcademicYears = new SelectList(years, "Id", "Name");
            model.Courses = new SelectList(courses, "Id", "Name");
            model.NextCourses = new SelectList(courses, "Id", "Name");

            // Classes and Divisions will be populated via AJAX
            model.Classes = new SelectList(Enumerable.Empty<SelectListItem>());
            model.NextClasses = new SelectList(Enumerable.Empty<SelectListItem>());
            model.Divisions = new SelectList(Enumerable.Empty<SelectListItem>());
            model.NextDivisions = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        private string GetNextClassName(string currentClass)
        {
            if (string.IsNullOrEmpty(currentClass)) return "";

            // Example: "FY" -> "SY", "SY" -> "TY"
            switch (currentClass)
            {
                case var c when c.StartsWith("FY"): return c.Replace("FY", "SY");
                case var c when c.StartsWith("SY"): return c.Replace("SY", "TY");
                default: return currentClass;
            }
        }

        private async Task ArchiveAttendanceRecords(int studentId, int academicYearId, string userName)
        {
            var records = await _context.AttendanceRecords
                .Include(ar => ar.Student)
                .Include(ar => ar.Subject)
                .Where(ar => ar.StudentId == studentId)
                .ToListAsync();

            var archiveRecords = records.Select(r => new StudentAttendanceArchive
            {
                StudentId = r.StudentId,
                EnrollmentNo = r.Student.EnrollmentNo,
                StudentName = r.Student.Name,
                SubjectId = r.SubjectId,
                SubjectName = r.Subject.Name,
                Date = r.Date,
                IsPresent = r.IsPresent,
                AcademicYearId = academicYearId,
                MarkedById = r.MarkedById,
                ArchivedOn = DateTime.Now
            });

            // Remove old records and add archive records
            _context.AttendanceRecords.RemoveRange(records);
            await _context.StudentAttendanceArchives.AddRangeAsync(archiveRecords);
        }
    }
}
