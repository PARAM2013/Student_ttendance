using Microsoft.AspNetCore.Mvc;
using Student_Attendance.ViewModels;
using Student_Attendance.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Student_Attendance.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml; // Add this at the top

namespace Student_Attendance.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(ApplicationDbContext context, ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    EnrollmentNo = s.EnrollmentNo,
                    Name = s.Name,
                    Cast = s.Cast,
                    Email = s.Email,
                    Mobile = s.Mobile,
                    CourseId = s.CourseId,
                    Semester = s.Semester,
                    IsActive = s.IsActive,
                    AcademicYearId = s.AcademicYearId,
                    DivisionId = s.DivisionId,
                    Course = s.Course,
                    AcademicYear = s.AcademicYear,
                    Division = s.Division
                })
                .ToListAsync();

            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentViewModel
            {
                Id = student.Id,
                EnrollmentNo = student.EnrollmentNo,
                Name = student.Name,
                Cast = student.Cast,
                Email = student.Email,
                Mobile = student.Mobile,
                CourseId = student.CourseId,
                Semester = student.Semester,
                IsActive = student.IsActive,
                AcademicYearId = student.AcademicYearId,
                DivisionId = student.DivisionId,
                Course = student.Course,
                AcademicYear = student.AcademicYear,
                Division = student.Division
            };

            return View(viewModel);
        }

        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new StudentViewModel();
            await LoadStudentDropDowns(viewModel);
            return View(viewModel);
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var student = new Student
                    {
                        EnrollmentNo = model.EnrollmentNo,
                        Name = model.Name,
                        Cast = model.Cast,
                        Email = model.Email,
                        Mobile = model.Mobile,
                        CourseId = model.CourseId,
                        Semester = model.Semester,
                        IsActive = model.IsActive,
                        AcademicYearId = model.AcademicYearId,
                        DivisionId = model.DivisionId,
                        ClassId = model.ClassId
                    };

                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    // After saving the student, get all subjects for the selected class and semester
                    var subjects = await _context.Subjects
                        .Where(s => s.ClassId == model.ClassId && 
                                   s.Semester == model.Semester)
                        .ToListAsync();

                    // Create subject mappings for the student
                    if (subjects.Any())
                    {
                        var studentSubjects = subjects.Select(subject => new StudentSubject
                        {
                            StudentId = student.Id,
                            SubjectId = subject.Id
                        });

                        await _context.StudentSubjects.AddRangeAsync(studentSubjects);
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Student created successfully with subject mappings!" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student");
                    return Json(new { success = false, message = "An error occurred while creating the student", error = ex.Message });
                }
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Validation failed", errors = errors });
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentViewModel
            {
                Id = student.Id,
                EnrollmentNo = student.EnrollmentNo,
                Name = student.Name,
                Cast = student.Cast,
                Email = student.Email,
                Mobile = student.Mobile,
                CourseId = student.CourseId,
                Semester = student.Semester,
                IsActive = student.IsActive,
                AcademicYearId = student.AcademicYearId,
                DivisionId = student.DivisionId
            };

            await LoadStudentDropDowns(viewModel);
            return View(viewModel);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _context.Students.FindAsync(id);
                    if (student == null)
                    {
                        return NotFound();
                    }

                    student.EnrollmentNo = model.EnrollmentNo;
                    student.Name = model.Name;
                    student.Cast = model.Cast;
                    student.Email = model.Email;
                    student.Mobile = model.Mobile;
                    student.CourseId = model.CourseId;
                    student.Semester = model.Semester;
                    student.IsActive = model.IsActive;
                    student.AcademicYearId = model.AcademicYearId;
                    student.DivisionId = model.DivisionId;

                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Student updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Validation failed", errors = errors });
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }

        private async Task LoadStudentDropDowns(StudentViewModel model)
        {
            model.Courses = await _context.Courses.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync();

            model.AcademicYears = await _context.AcademicYears.Select(ay => new SelectListItem
            {
                Value = ay.Id.ToString(),
                Text = ay.Name
            }).ToListAsync();

            model.Divisions = await _context.Divisions.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

            model.Classes = await _context.Classes
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} ({c.Course.Name})"
                }).ToListAsync();

            // Reset Divisions dropdown when loading initially
            model.Divisions = new List<SelectListItem>();
        }

        [HttpGet]
        public async Task<IActionResult> GetDivisionsByClass(int classId)
        {
            var divisions = await _context.Divisions
                .Where(d => d.ClassId == classId)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToListAsync();

            return Json(divisions);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectMapping(int id)
        {
            try
            {
                // Get student's course
                var student = await _context.Students
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                // Get available subjects for student's course
                var availableSubjects = await _context.Subjects
                    .Where(s => s.CourseId == student.CourseId && 
                               s.Semester == student.Semester)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Code,
                        s.Semester
                    })
                    .ToListAsync();

                // Get currently mapped subjects
                var mappedSubjectIds = await _context.StudentSubjects
                    .Where(ss => ss.StudentId == id)
                    .Select(ss => ss.SubjectId)
                    .ToListAsync();

                ViewBag.AvailableSubjects = availableSubjects;
                ViewBag.MappedSubjects = mappedSubjectIds;

                return PartialView("_SubjectMapping", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subject mapping for student {StudentId}", id);
                return Json(new { success = false, message = "Error loading subjects" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSubjectMapping(int studentId, List<int> subjectIds)
        {
            try
            {
                // Remove existing mappings
                var existingMappings = await _context.StudentSubjects
                    .Where(ss => ss.StudentId == studentId)
                    .ToListAsync();
                
                _context.StudentSubjects.RemoveRange(existingMappings);

                // Add new mappings
                if (subjectIds != null && subjectIds.Any())
                {
                    var newMappings = subjectIds.Select(subjectId => new StudentSubject
                    {
                        StudentId = studentId,
                        SubjectId = subjectId
                    });

                    await _context.StudentSubjects.AddRangeAsync(newMappings);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Subject mapping updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subject mapping for student {StudentId}", studentId);
                return Json(new { success = false, message = "Error saving subject mapping" });
            }
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new StudentImportViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Import(StudentImportViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file to import");
                return View(model);
            }

            if (!Path.GetExtension(model.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("File", "Please select an Excel file (.xlsx)");
                return View(model);
            }

            try
            {
                using var stream = model.File.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip header
                {
                    try
                    {
                        string enrollmentNo = worksheet.Cells[row, 1].Value?.ToString();
                        string studentName = worksheet.Cells[row, 2].Value?.ToString();
                        string division = worksheet.Cells[row, 3].Value?.ToString();
                        string course = worksheet.Cells[row, 4].Value?.ToString();
                        string semester = worksheet.Cells[row, 5].Value?.ToString();
                        string email = worksheet.Cells[row, 6].Value?.ToString();
                        string mobile = worksheet.Cells[row, 7].Value?.ToString();
                        string cast = worksheet.Cells[row, 8].Value?.ToString();
                        string academicYear = worksheet.Cells[row, 9].Value?.ToString();

                        // Validate required fields
                        if (string.IsNullOrEmpty(enrollmentNo) || string.IsNullOrEmpty(studentName) ||
                            string.IsNullOrEmpty(division) || string.IsNullOrEmpty(course) ||
                            string.IsNullOrEmpty(semester) || string.IsNullOrEmpty(academicYear))
                        {
                            model.ImportErrors.Add($"Row {row}: Missing required fields");
                            model.ErrorCount++;
                            continue;
                        }

                        // Get references
                        var courseEntity = await _context.Courses.FirstOrDefaultAsync(c => c.Name == course);
                        var divisionEntity = await _context.Divisions.FirstOrDefaultAsync(d => d.Name == division);
                        var academicYearEntity = await _context.AcademicYears.FirstOrDefaultAsync(ay => ay.Name == academicYear);

                        if (courseEntity == null || divisionEntity == null || academicYearEntity == null)
                        {
                            model.ImportErrors.Add($"Row {row}: Invalid Course, Division or Academic Year");
                            model.ErrorCount++;
                            continue;
                        }

                        // Check if enrollment number already exists
                        if (await _context.Students.AnyAsync(s => s.EnrollmentNo == enrollmentNo))
                        {
                            model.ImportErrors.Add($"Row {row}: Enrollment number {enrollmentNo} already exists");
                            model.ErrorCount++;
                            continue;
                        }

                        // Create new student
                        var student = new Student
                        {
                            EnrollmentNo = enrollmentNo,
                            Name = studentName,
                            DivisionId = divisionEntity.Id,
                            CourseId = courseEntity.Id,
                            Semester = int.Parse(semester),
                            Email = email,
                            Mobile = mobile,
                            Cast = cast,
                            AcademicYearId = academicYearEntity.Id,
                            IsActive = true
                        };

                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                        model.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        model.ImportErrors.Add($"Row {row}: {ex.Message}");
                        model.ErrorCount++;
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                model.ImportErrors.Add($"Error processing file: {ex.Message}");
                return View(model);
            }
        }

        // Add this new method to get subjects for a class and semester
        [HttpGet]
        public async Task<IActionResult> GetSubjectsForClass(int classId, int semester)
        {
            var subjects = await _context.Subjects
                .Where(s => s.ClassId == classId && s.Semester == semester)
                .Select(s => new { s.Id, s.Name, s.Code })
                .ToListAsync();

            return Json(new { success = true, subjects = subjects });
        }
    }
}