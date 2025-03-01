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
        public async Task<IActionResult> GetPreviewData(int currentYearId, int nextYearId, int courseId, int? classId, int? divisionId)
        {
            try
            {
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
                if (!model.StudentsToPromote.Any())
                    return Json(new { success = false, message = "No students selected for promotion" });

                // Start transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Archive attendance records
                    await ArchiveAttendanceRecords(model.CurrentAcademicYearId, model.StudentsToPromote.Select(s => s.StudentId).ToList());

                    // Prepare students for next year
                    foreach (var student in model.StudentsToPromote.Where(s => s.Selected))
                    {
                        var existingStudent = await _context.Students.FindAsync(student.StudentId);
                        if (existingStudent != null)
                        {
                            // Update student record
                            existingStudent.AcademicYearId = model.NextAcademicYearId;
                            existingStudent.Semester = student.NextSemester;
                            
                            // Update class if needed
                            var nextClass = await _context.Classes
                                .FirstOrDefaultAsync(c => c.Name == student.NextClass && 
                                                        c.CourseId == existingStudent.CourseId);
                            if (nextClass != null)
                            {
                                existingStudent.ClassId = nextClass.Id;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Data moved successfully" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving data");
                return Json(new { success = false, message = "Error moving data" });
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

        private async Task LoadDropdowns(StudentCarryForwardViewModel model)
        {
            var years = await _context.AcademicYears
                .OrderByDescending(y => y.StartDate)
                .ToListAsync();

            model.AcademicYears = new SelectList(years, "Id", "Name");
            model.NextAcademicYears = new SelectList(years, "Id", "Name");
            model.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
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
