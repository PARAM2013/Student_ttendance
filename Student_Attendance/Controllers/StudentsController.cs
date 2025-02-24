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
    // Change inheritance:
    public class StudentsController : BaseController
    {
        private readonly ILogger<StudentsController> _logger;

        // Update constructor to call base constructor
        public StudentsController(ApplicationDbContext context, ILogger<StudentsController> logger)
            : base(context)
        {
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(int? yearId, int? courseId, int? divisionId, bool? isActive)
        {
            // Start with base query
            var query = _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .AsQueryable();

            // Apply filters
            if (yearId.HasValue)
            {
                query = query.Where(s => s.AcademicYearId == yearId);
            }

            if (courseId.HasValue)
            {
                query = query.Where(s => s.CourseId == courseId);
            }

            if (divisionId.HasValue)
            {
                query = query.Where(s => s.DivisionId == divisionId);
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var students = await query
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

            // Load dropdown data
            ViewBag.Years = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            ViewBag.Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name");

            // Set selected values
            ViewBag.SelectedYear = yearId;
            ViewBag.SelectedCourse = courseId;
            ViewBag.SelectedDivision = divisionId;
            ViewBag.SelectedStatus = isActive;

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
                        CourseId = model.CourseId ?? 0,  // Add null check
                        Semester = model.Semester ?? 0,   // Add null check
                        IsActive = model.IsActive,
                        AcademicYearId = model.AcademicYearId ?? 0,  // Add null check
                        DivisionId = model.DivisionId ?? 0,          // Add null check
                        ClassId = model.ClassId ?? 0                 // Add null check
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

            var student = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .Include(s => s.Class)  // Make sure Class is included
                .FirstOrDefaultAsync(s => s.Id == id);

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
                ClassId = student.ClassId,  // Set the ClassId
                Semester = student.Semester,
                IsActive = student.IsActive,
                AcademicYearId = student.AcademicYearId,
                DivisionId = student.DivisionId,
                Course = student.Course,
                Class = student.Class,     // Set the Class
                AcademicYear = student.AcademicYear,
                Division = student.Division
            };

            await LoadStudentDropDowns(viewModel);

            // If a division is selected, populate divisions for the selected class
            if (student.ClassId > 0)
            {
                viewModel.Divisions = await _context.Divisions
                    .Where(d => d.ClassId == student.ClassId)
                    .Select(d => new SelectListItem 
                    { 
                        Value = d.Id.ToString(), 
                        Text = d.Name,
                        Selected = d.Id == student.DivisionId
                    })
                    .ToListAsync();
            }

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
                    student.CourseId = model.CourseId ?? 0;  // Add null check
                    student.Semester = model.Semester ?? 0;   // Add null check
                    student.IsActive = model.IsActive;
                    student.AcademicYearId = model.AcademicYearId ?? 0;  // Add null check
                    student.DivisionId = model.DivisionId ?? 0;          // Add null check

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
            // Load courses with selected state
            model.Courses = await _context.Courses
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == model.CourseId
                })
                .ToListAsync();

            // Load academic years with selected state
            model.AcademicYears = await _context.AcademicYears
                .Select(ay => new SelectListItem
                {
                    Value = ay.Id.ToString(),
                    Text = ay.Name,
                    Selected = ay.Id == model.AcademicYearId
                })
                .ToListAsync();

            // Load classes with selected state
            model.Classes = await _context.Classes
                .Include(c => c.Course)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} ({c.Course.Name})",
                    Selected = c.Id == model.ClassId
                })
                .ToListAsync();

            // Only populate divisions if a class is selected
            if (model.ClassId.HasValue)
            {
                model.Divisions = await _context.Divisions
                    .Where(d => d.ClassId == model.ClassId)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name,
                        Selected = d.Id == model.DivisionId
                    })
                    .ToListAsync();
            }
            else
            {
                model.Divisions = new List<SelectListItem>();
            }
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

            try
            {
                using var stream = model.File.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var enrollmentNo = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrEmpty(enrollmentNo)) continue;

                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => s.EnrollmentNo == enrollmentNo);
                        
                        bool isNewStudent = student == null;
                        if (isNewStudent)
                        {
                            student = new Student();
                        }

                        // Update student properties
                        student.EnrollmentNo = enrollmentNo;
                        student.Name = worksheet.Cells[row, 2].Value?.ToString();
                        student.Email = worksheet.Cells[row, 3].Value?.ToString();
                        student.Mobile = worksheet.Cells[row, 4].Value?.ToString();
                        student.Cast = worksheet.Cells[row, 5].Value?.ToString();

                        // Get or create related entities
                        var courseName = worksheet.Cells[row, 6].Value?.ToString();
                        var className = worksheet.Cells[row, 7].Value?.ToString();
                        var divisionName = worksheet.Cells[row, 8].Value?.ToString();
                        var semester = Convert.ToInt32(worksheet.Cells[row, 9].Value);
                        var academicYearName = worksheet.Cells[row, 10].Value?.ToString();

                        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Name == courseName);
                        var division = await _context.Divisions.FirstOrDefaultAsync(d => d.Name == divisionName);
                        var academicYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.Name == academicYearName);
                        var class_ = !string.IsNullOrEmpty(className) ? 
                            await _context.Classes.FirstOrDefaultAsync(c => c.Name == className) : null;

                        if (course == null || division == null || academicYear == null)
                        {
                            throw new Exception("Invalid Course, Division or Academic Year");
                        }

                        student.CourseId = course.Id;
                        student.DivisionId = division.Id;
                        student.AcademicYearId = academicYear.Id;
                        student.ClassId = class_?.Id ?? 0;
                        student.Semester = semester;
                        student.IsActive = true;

                        if (isNewStudent)
                        {
                            await _context.Students.AddAsync(student);
                        }
                        else
                        {
                            _context.Students.Update(student);
                        }

                        await _context.SaveChangesAsync();

                        // Map subjects if class is specified
                        if (class_ != null)
                        {
                            var subjects = await _context.Subjects
                                .Where(s => s.ClassId == class_.Id && s.Semester == semester)
                                .ToListAsync();

                            foreach (var subject in subjects)
                            {
                                if (!await _context.StudentSubjects.AnyAsync(ss => 
                                    ss.StudentId == student.Id && ss.SubjectId == subject.Id))
                                {
                                    await _context.StudentSubjects.AddAsync(new StudentSubject
                                    {
                                        StudentId = student.Id,
                                        SubjectId = subject.Id
                                    });
                                }
                            }
                            await _context.SaveChangesAsync();
                        }

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

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Students");
                
                // Add headers
                var headers = new[] { 
                    "EnrollmentNo*", "Name*", "Email", "Mobile", "Cast", 
                    "Course*", "Class", "Division*", "Semester*", "AcademicYear*" 
                };
                
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.SetBackground(System.Drawing.Color.LightGray);
                }

                // Add validation lists
                var courses = await _context.Courses.Select(c => c.Name).ToListAsync();
                var classes = await _context.Classes.Select(c => c.Name).ToListAsync();
                var divisions = await _context.Divisions.Select(d => d.Name).ToListAsync();
                var academicYears = await _context.AcademicYears.Select(a => a.Name).ToListAsync();

                var coursesSheet = package.Workbook.Worksheets.Add("ValidLists");
                coursesSheet.Hidden = eWorkSheetHidden.VeryHidden;
                
                // Add validation data to hidden sheet
                for (int i = 0; i < courses.Count; i++)
                    coursesSheet.Cells[i + 1, 1].Value = courses[i];
                for (int i = 0; i < classes.Count; i++)
                    coursesSheet.Cells[i + 1, 2].Value = classes[i];
                for (int i = 0; i < divisions.Count; i++)
                    coursesSheet.Cells[i + 1, 3].Value = divisions[i];
                for (int i = 0; i < academicYears.Count; i++)
                    coursesSheet.Cells[i + 1, 4].Value = academicYears[i];

                // Add data validation to main sheet
                var courseRange = worksheet.DataValidations.AddListValidation("F2:F1000");
                courseRange.Formula.ExcelFormula = $"=ValidLists!$A$1:$A${courses.Count}";

                var classRange = worksheet.DataValidations.AddListValidation("G2:G1000");
                classRange.Formula.ExcelFormula = $"=ValidLists!$B$1:$B${classes.Count}";

                var divisionRange = worksheet.DataValidations.AddListValidation("H2:H1000");
                divisionRange.Formula.ExcelFormula = $"=ValidLists!$C$1:$C${divisions.Count}";

                var academicYearRange = worksheet.DataValidations.AddListValidation("J2:J1000");
                academicYearRange.Formula.ExcelFormula = $"=ValidLists!$D$1:$D${academicYears.Count}";

                worksheet.Cells.AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentTemplate.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadData()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Students");

                // Add headers
                var headers = new[] { 
                    "EnrollmentNo", "Name", "Email", "Mobile", "Cast", 
                    "Course", "Class", "Division", "Semester", "AcademicYear" 
                };
                
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                var students = await _context.Students
                    .Include(s => s.Course)
                    .Include(s => s.Class)
                    .Include(s => s.Division)
                    .Include(s => s.AcademicYear)
                    .ToListAsync();

                int row = 2;
                foreach (var student in students)
                {
                    worksheet.Cells[row, 1].Value = student.EnrollmentNo;
                    worksheet.Cells[row, 2].Value = student.Name;
                    worksheet.Cells[row, 3].Value = student.Email;
                    worksheet.Cells[row, 4].Value = student.Mobile;
                    worksheet.Cells[row, 5].Value = student.Cast;
                    worksheet.Cells[row, 6].Value = student.Course?.Name;
                    worksheet.Cells[row, 7].Value = student.Class?.Name;
                    worksheet.Cells[row, 8].Value = student.Division?.Name;
                    worksheet.Cells[row, 9].Value = student.Semester;
                    worksheet.Cells[row, 10].Value = student.AcademicYear?.Name;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"Students_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var student = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentViewModel
            {
                Id = student.Id,
                SSID = student.SSID,  // Add this line
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
                Division = student.Division,
                Class = student.Class
            };

            return PartialView("_StudentDetailsPartial", viewModel);
        }

        // Add this new action for getting divisions by course
        [HttpGet]
        public async Task<IActionResult> GetDivisionsForCourse(int courseId)
        {
            var divisions = await _context.Divisions
                .Where(d => d.Class.CourseId == courseId)
                .Select(d => new { id = d.Id, name = d.Name })
                .Distinct()
                .ToListAsync();
            return Json(divisions);
        }
    }
}