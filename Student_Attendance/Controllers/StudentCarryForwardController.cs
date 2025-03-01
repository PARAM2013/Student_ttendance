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
        public async Task<IActionResult> GetPreviewData(int currentYearId, int nextYearId, int courseId, int? classId, int? divisionId, int nextCourseId, int? nextClassId, int? nextDivisionId)
        {
            try
            {
                // Validate next year configurations
                var nextClassExists = await _context.Classes
                    .AnyAsync(c => c.Id == nextClassId && 
                                  c.AcademicYearId == nextYearId && 
                                  c.CourseId == nextCourseId);

                if (!nextClassExists)
                {
                    return Json(new { success = false, message = "Selected next class configuration is invalid" });
                }

                var query = _context.Students
                    .Include(s => s.Class)
                    .Where(s => s.AcademicYearId == currentYearId && 
                               s.CourseId == courseId && 
                               s.IsActive);

                if (classId.HasValue)
                    query = query.Where(s => s.ClassId == classId.Value);
                if (divisionId.HasValue)
                    query = query.Where(s => s.DivisionId == divisionId.Value);

                var students = await query.ToListAsync();

                var promotionData = students.Select(s => new StudentPromotionData
                {
                    StudentId = s.Id,
                    EnrollmentNo = s.EnrollmentNo,
                    Name = s.Name,
                    CurrentSemester = s.Semester,
                    NextSemester = s.Semester + 1,
                    CurrentClass = s.Class?.Name ?? "N/A",
                    NextClass = GetNextClassName(s.Class?.Name ?? ""),
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
                if (!model.StudentsToPromote?.Any() ?? true)
                {
                    return Json(new { success = false, message = "No students selected for promotion" });
                }

                if (model.CurrentAcademicYearId == 0 || model.NextAcademicYearId == 0)
                {
                    return Json(new { success = false, message = "Invalid academic year selection" });
                }

                if (model.NextClassId == null)
                {
                    return Json(new { success = false, message = "Next class must be selected" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var modifiedCount = 0;
                    foreach (var studentData in model.StudentsToPromote.Where(s => s.Selected))
                    {
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => s.Id == studentData.StudentId && s.IsActive);

                        if (student != null)
                        {
                            _logger.LogInformation($"Updating student {student.Id}: {student.Name}");
                            
                            // Update student record
                            student.AcademicYearId = model.NextAcademicYearId;
                            student.Semester = studentData.NextSemester;
                            student.ClassId = model.NextClassId.Value;
                            
                            // Only update division if specified
                            if (model.NextDivisionId.HasValue)
                            {
                                student.DivisionId = model.NextDivisionId.Value;
                            }

                            _context.Students.Update(student);
                            modifiedCount++;
                        }
                        else
                        {
                            _logger.LogWarning($"Student with ID {studentData.StudentId} not found or not active");
                        }
                    }

                    if (modifiedCount == 0)
                    {
                        throw new Exception("No students were updated");
                    }

                    // Archive attendance records
                    await ArchiveAttendanceRecords(model.CurrentAcademicYearId, 
                        model.StudentsToPromote.Select(s => s.StudentId).ToList());

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { 
                        success = true, 
                        message = $"Successfully moved {modifiedCount} students to next year" 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while moving students");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving students to next year");
                return Json(new { 
                    success = false, 
                    message = "Failed to move students: " + ex.Message 
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

        private async Task ArchiveAttendanceRecords(int academicYearId, List<int> studentIds)
        {
            var records = await _context.AttendanceRecords
                .Include(ar => ar.Student)
                .Include(ar => ar.Subject)
                .Where(ar => studentIds.Contains(ar.StudentId))
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

            await _context.StudentAttendanceArchives.AddRangeAsync(archiveRecords);
        }
    }
}
