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
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Student_Attendance.Services.Logging;

namespace Student_Attendance.Controllers
{
    // Change inheritance:
    public class StudentsController : BaseController
    {
        private readonly ILogger<StudentsController> _logger;
        private readonly ILoggingService _loggingService;

        // Update constructor to call base constructor
        public StudentsController(ApplicationDbContext context, ILogger<StudentsController> logger, ILoggingService loggingService)
            : base(context)
        {
            _logger = logger;
            _loggingService = loggingService;
        }

        // GET: Students
        public async Task<IActionResult> Index(int? yearId, int? courseId, int? divisionId, bool? isActive)
        {
            // Start with base query
            var query = _context.Students
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Division)
                .Include(s => s.Class) // Add Class include
                .OrderByDescending(s => s.Id) // Show newest students first
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

            var students = await query.Select(s => new StudentViewModel
            {
                Id = s.Id,
                SSID = s.SSID,
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
                Class = s.Class,
                AcademicYear = s.AcademicYear,
                Division = s.Division
            }).ToListAsync();

            // Load dropdown data
            ViewBag.Years = new SelectList(await _context.AcademicYears
                .OrderByDescending(y => y.IsActive)
                .ThenByDescending(y => y.Name)
                .ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _context.Courses.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewBag.Divisions = new SelectList(await _context.Divisions.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");

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
            try
            {
                if (!ModelState.IsValid)
                {
                    var validationErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Validation failed", errors = validationErrors });
                }

                // Verify all required relationships exist
                var course = await _context.Courses.FindAsync(model.CourseId);
                var academicYear = await _context.AcademicYears.FindAsync(model.AcademicYearId);
                var class_ = await _context.Classes.FindAsync(model.ClassId);
                var division = await _context.Divisions.FindAsync(model.DivisionId);

                if (course == null || academicYear == null || class_ == null || division == null)
                {
                    return Json(new { success = false, message = "One or more required relationships not found" });
                }

                // Check if enrollment number is unique
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.EnrollmentNo == model.EnrollmentNo);
                
                if (existingStudent != null)
                {
                    return Json(new { success = false, message = "Enrollment number already exists" });
                }

                // Generate SSID with shorter format
                var lastStudent = await _context.Students
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefaultAsync();
                
                int nextNumber = (lastStudent?.Id ?? 0) + 1;
                string ssid = $"S{nextNumber:D3}"; // This will generate SSID like S001, S002, etc.

                // Create new student
                var student = new Student
                {
                    SSID = ssid, // Add the generated SSID
                    EnrollmentNo = model.EnrollmentNo,
                    Name = model.Name,
                    Cast = model.Cast,
                    Email = model.Email,
                    Mobile = model.Mobile,
                    CourseId = model.CourseId.Value,
                    ClassId = model.ClassId.Value,
                    Semester = model.Semester.Value,
                    IsActive = model.IsActive,
                    AcademicYearId = model.AcademicYearId.Value,
                    DivisionId = model.DivisionId.Value
                };

                try
                {
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // After successfully saving student, map subjects
                    var subjects = await _context.Subjects
                        .Where(s => s.ClassId == model.ClassId && s.Semester == model.Semester)
                        .ToListAsync();

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

                    await _loggingService.LogActivityAsync(
                        action: "Create Student",
                        entityType: "Student",
                        entityId: student.Id.ToString(),
                        details: $"Created new student: {student.Name}",
                        module: "Student Management",
                        isSuccess: true
                    );

                    return Json(new { success = true, message = "Student created successfully!" });
                }
                catch (DbUpdateException ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    _logger.LogError(ex, "Database error while creating student: {Error}", innerMessage);
                    await _loggingService.LogErrorAsync(
                        errorMessage: innerMessage,
                        stackTrace: ex.StackTrace,
                        errorType: ex.GetType().Name,
                        source: "StudentsController.Create"
                    );
                    return Json(new { success = false, message = $"Database error: {innerMessage}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                await _loggingService.LogErrorAsync(
                    errorMessage: ex.Message,
                    stackTrace: ex.StackTrace,
                    errorType: ex.GetType().Name,
                    source: "StudentsController.Create"
                );
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Error creating student: {errorMessage}" });
            }
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
                SSID = student.SSID,  // Add this line
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

                    var oldName = student.Name;

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
                    student.ClassId = model.ClassId ?? 0; // Add this line

                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    await _loggingService.LogActivityAsync(
                        action: "Edit Student",
                        entityType: "Student",
                        entityId: id.ToString(),
                        details: $"Updated student: {model.Name}",
                        oldValue: oldName,
                        newValue: model.Name,
                        module: "Student Management",
                        isSuccess: true
                    );

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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating student");
                    await _loggingService.LogErrorAsync(
                        errorMessage: ex.Message,
                        stackTrace: ex.StackTrace,
                        errorType: ex.GetType().Name,
                        source: "StudentsController.Edit"
                    );
                    return Json(new { success = false, message = "An error occurred while updating the student", error = ex.Message });
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

            // Only load classes if course is selected
            if (model.CourseId.HasValue)
            {
                model.Classes = await _context.Classes
                    .Where(c => c.CourseId == model.CourseId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == model.ClassId
                    })
                    .ToListAsync();
            }
            else
            {
                model.Classes = new List<SelectListItem>();
            }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSubjectMapping(int studentId, List<int> subjectIds)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data submitted" });
                }

                // Get student to verify it exists
                var student = await _context.Students
                    .Include(s => s.StudentSubjects)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Remove existing mappings
                    _context.StudentSubjects.RemoveRange(student.StudentSubjects);

                    // Add new mappings
                    if (subjectIds?.Any() == true)
                    {
                        var newMappings = subjectIds.Select(subjectId => new StudentSubject
                        {
                            StudentId = studentId,
                            SubjectId = subjectId
                        });
                        await _context.StudentSubjects.AddRangeAsync(newMappings);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Subject mapping updated successfully" });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
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

            string tempPath = "";
            try
            {
                // Create a preview of the import
                var previewModel = await ValidateAndPreviewImport(model.File);
                
                // Store the uploaded file temporarily with a unique ID
                var fileId = Guid.NewGuid().ToString();
                tempPath = Path.Combine(Path.GetTempPath(), fileId + ".xlsx");
                
                // Use using block to ensure proper disposal of FileStream
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    await model.File.CopyToAsync(fileStream);
                }
                
                previewModel.FileId = fileId;
                return View("ImportPreview", previewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import preview");
                
                // Clean up temp file if it exists
                if (!string.IsNullOrEmpty(tempPath) && System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete temporary file: {TempPath}", tempPath);
                    }
                }
                
                model.ImportErrors.Add($"Error previewing import: {ex.Message}");
                return View(model);
            }
        }

        private async Task<ImportPreviewViewModel> ValidateAndPreviewImport(IFormFile file)
        {
            var preview = new ImportPreviewViewModel();
            var processedEnrollmentNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            
            if (worksheet?.Dimension == null)
                throw new Exception("The Excel file is empty or invalid");

            preview.TotalRows = worksheet.Dimension.Rows - 1; // Excluding header

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var studentRow = new StudentImportRow { RowNumber = row };

                try
                {
                    // Clean and trim data
                    studentRow.EnrollmentNo = (worksheet.Cells[row, 2].Value?.ToString() ?? "").Trim();
                    studentRow.Name = (worksheet.Cells[row, 3].Value?.ToString() ?? "").Trim();
                    studentRow.Email = (worksheet.Cells[row, 4].Value?.ToString() ?? "").Trim();
                    studentRow.Mobile = (worksheet.Cells[row, 5].Value?.ToString() ?? "").Trim();
                    studentRow.Cast = (worksheet.Cells[row, 6].Value?.ToString() ?? "").Trim();
                    studentRow.Course = (worksheet.Cells[row, 7].Value?.ToString() ?? "").Trim();
                    studentRow.Class = (worksheet.Cells[row, 8].Value?.ToString() ?? "").Trim();
                    studentRow.Division = (worksheet.Cells[row, 9].Value?.ToString() ?? "").Trim();
                    
                    var semesterStr = worksheet.Cells[row, 10].Value?.ToString();
                    studentRow.Semester = semesterStr != null ? int.Parse(semesterStr) : null;
                    
                    studentRow.AcademicYear = (worksheet.Cells[row, 11].Value?.ToString() ?? "").Trim();

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(studentRow.EnrollmentNo) && 
                        string.IsNullOrWhiteSpace(studentRow.Name))
                        continue;

                    // Check for duplicates within the file
                    if (!string.IsNullOrEmpty(studentRow.EnrollmentNo) && 
                        !processedEnrollmentNos.Add(studentRow.EnrollmentNo))
                    {
                        studentRow.Status = ImportRowStatus.Duplicate;
                        studentRow.StatusMessage = "Duplicate enrollment number in file";
                        preview.DuplicatesInFile++;
                    }
                    else
                    {
                        // Check if student exists in database
                        var existingStudent = await _context.Students
                            .FirstOrDefaultAsync(s => s.EnrollmentNo == studentRow.EnrollmentNo);

                        if (existingStudent != null)
                        {
                            studentRow.Status = ImportRowStatus.Update;
                            preview.UpdatedStudents++;
                        }
                        else
                        {
                            studentRow.Status = ImportRowStatus.New;
                            preview.NewStudents++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    studentRow.Status = ImportRowStatus.Error;
                    studentRow.StatusMessage = ex.Message;
                    preview.ValidationErrors.Add($"Row {row}: {ex.Message}");
                }

                preview.Students.Add(studentRow);
            }

            return preview;
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmImport(string fileId)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), fileId + ".xlsx");
            var importStats = new ImportStats(); // Track import statistics

            try
            {
                if (!System.IO.File.Exists(tempPath))
                {
                    TempData["Error"] = "Import file not found. Please try again.";
                    return RedirectToAction("Import");
                }

                // Read all bytes at once and create new MemoryStream to avoid file locks
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(tempPath);
                using var memoryStream = new MemoryStream(fileBytes);
                using var package = new ExcelPackage(memoryStream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet == null || worksheet.Dimension == null)
                {
                    TempData["Error"] = "The Excel file is invalid or empty";
                    return RedirectToAction("Import");
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var existingStudents = await _context.Students
                            .ToDictionaryAsync(s => s.EnrollmentNo.ToLower());

                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            var enrollmentNo = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(enrollmentNo)) continue;

                            var student = existingStudents.GetValueOrDefault(enrollmentNo.ToLower());
                            bool isNewStudent = student == null;

                            try
                            {
                                if (isNewStudent)
                                {
                                    student = new Student();
                                    var lastStudent = await _context.Students
                                        .OrderByDescending(s => s.Id)
                                        .FirstOrDefaultAsync();

                                    int nextNumber = (lastStudent?.Id ?? 0) + 1;
                                    student.SSID = $"S{nextNumber:D3}";
                                    student.EnrollmentNo = enrollmentNo;
                                    student.IsActive = true;
                                }

                                // Update student properties
                                student.Name = (worksheet.Cells[row, 3].Value?.ToString() ?? "").Trim();
                                student.Email = (worksheet.Cells[row, 4].Value?.ToString() ?? "").Trim();
                                student.Mobile = (worksheet.Cells[row, 5].Value?.ToString() ?? "").Trim();
                                student.Cast = (worksheet.Cells[row, 6].Value?.ToString() ?? "").Trim();
                                student.Semester = Convert.ToInt32(worksheet.Cells[row, 10].Value);

                                var courseName = (worksheet.Cells[row, 7].Value?.ToString() ?? "").Trim();
                                var className = (worksheet.Cells[row, 8].Value?.ToString() ?? "").Trim();
                                var divisionName = (worksheet.Cells[row, 9].Value?.ToString() ?? "").Trim();
                                var academicYearName = (worksheet.Cells[row, 11].Value?.ToString() ?? "").Trim();

                                // Find related entities
                                var course = await _context.Courses
                                    .FirstOrDefaultAsync(c => c.Name.ToLower() == courseName.ToLower());
                                var class_ = await _context.Classes
                                    .FirstOrDefaultAsync(c => c.Name.ToLower() == className.ToLower());
                                var division = await _context.Divisions
                                    .FirstOrDefaultAsync(d => d.Name.ToLower() == divisionName.ToLower());
                                var academicYear = await _context.AcademicYears
                                    .FirstOrDefaultAsync(a => a.Name.ToLower() == academicYearName.ToLower());

                                if (course == null || division == null || academicYear == null)
                                {
                                    importStats.ErrorCount++;
                                    continue;
                                }

                                student.CourseId = course.Id;
                                student.DivisionId = division.Id;
                                student.AcademicYearId = academicYear.Id;
                                student.ClassId = class_?.Id ?? 0;

                                if (isNewStudent)
                                {
                                    await _context.Students.AddAsync(student);
                                    importStats.NewCount++;
                                }
                                else
                                {
                                    _context.Students.Update(student);
                                    importStats.UpdateCount++;
                                }

                                await _context.SaveChangesAsync();

                                // Map subjects if class exists
                                if (class_ != null)
                                {
                                    await MapStudentSubjects(student.Id, class_.Id, student.Semester);
                                }

                                importStats.SuccessCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing row {Row}", row);
                                importStats.ErrorCount++;
                            }
                        }

                        await transaction.CommitAsync();

                        // Set appropriate message based on actual counts
                        if (importStats.ErrorCount == 0)
                        {
                            TempData["Success"] = $"Import completed successfully. Added {importStats.NewCount} new students and updated {importStats.UpdateCount} existing students.";
                        }
                        else
                        {
                            TempData["Warning"] = $"Import completed with {importStats.ErrorCount} errors. Successfully processed {importStats.SuccessCount} records (New: {importStats.NewCount}, Updated: {importStats.UpdateCount}).";
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import confirmation");
                TempData["Error"] = $"Import failed: {ex.Message}";
                return RedirectToAction("Import");
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {TempPath}", tempPath);
                }
            }
        }

        private class ImportStats
        {
            public int NewCount { get; set; }
            public int UpdateCount { get; set; }
            public int ErrorCount { get; set; }
            public int SuccessCount { get; set; }
        }

        private async Task MapStudentSubjects(int studentId, int classId, int semester)
        {
            // Get subjects for the class and semester
            var subjects = await _context.Subjects
                .Where(s => s.ClassId == classId && s.Semester == semester)
                .ToListAsync();

            // Remove existing mappings
            var existingMappings = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId)
                .ToListAsync();
            
            _context.StudentSubjects.RemoveRange(existingMappings);

            // Add new mappings
            if (subjects.Any())
            {
                var studentSubjects = subjects.Select(subject => new StudentSubject
                {
                    StudentId = studentId,
                    SubjectId = subject.Id
                });

                await _context.StudentSubjects.AddRangeAsync(studentSubjects);
                await _context.SaveChangesAsync();
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

                // Define headers with SSID first
                var headers = new[] { 
                    "SSID",
                    "EnrollmentNo*",
                    "Name*",
                    "Email",
                    "Mobile",
                    "Cast",
                    "Course*",
                    "Class*",
                    "Division*",
                    "Semester*", 
                    "AcademicYear*"
                };
                
                // Set up headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Get all students with related data
                var students = await _context.Students
                    .Include(s => s.Course)
                    .Include(s => s.Class)
                    .Include(s => s.Division)
                    .Include(s => s.AcademicYear)
                    .OrderByDescending(s => s.Id)
                    .ToListAsync();

                int row = 2;
                foreach (var student in students)
                {
                    worksheet.Cells[row, 1].Value = student.SSID;
                    worksheet.Cells[row, 2].Value = student.EnrollmentNo;
                    worksheet.Cells[row, 3].Value = student.Name;
                    worksheet.Cells[row, 4].Value = student.Email;
                    worksheet.Cells[row, 5].Value = student.Mobile;
                    worksheet.Cells[row, 6].Value = student.Cast;
                    worksheet.Cells[row, 7].Value = student.Course?.Name;
                    worksheet.Cells[row, 8].Value = student.Class?.Name;
                    worksheet.Cells[row, 9].Value = student.Division?.Name;
                    worksheet.Cells[row, 10].Value = student.Semester;
                    worksheet.Cells[row, 11].Value = student.AcademicYear?.Name;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var content = package.GetAsByteArray();
                var fileName = $"Students_Data_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

        [HttpGet]
        public async Task<IActionResult> GetClassesByCourse(int courseId)
        {
            var classes = await _context.Classes
                .Where(c => c.CourseId == courseId)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();
            return Json(classes);
        }

        [HttpGet]
        public async Task<IActionResult> SubjectsViaExcel()
        {
            var model = new SubjectMappingViewModel
            {
                Classes = await _context.Classes
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync(),
                Divisions = await _context.Divisions
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSubjectTemplate(int? classId = null, int? divisionId = null)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Students");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "Enrollment No";
                worksheet.Cells[1, 2].Value = "Student Name";
                worksheet.Cells[1, 3].Value = "Class";
                worksheet.Cells[1, 4].Value = "Division";
                worksheet.Cells[1, 5].Value = "Subject Codes (comma-separated)";

                // Style headers
                var headerRange = worksheet.Cells[1, 1, 1, 5];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                // Query students based on filters
                var query = _context.Students
                    .Include(s => s.Class)
                    .Include(s => s.Division)
                    .Include(s => s.StudentSubjects)
                        .ThenInclude(ss => ss.Subject)
                    .AsQueryable();

                if (classId.HasValue)
                    query = query.Where(s => s.ClassId == classId);
                if (divisionId.HasValue)
                    query = query.Where(s => s.DivisionId == divisionId);

                var students = await query.ToListAsync();

                // Add data
                int row = 2;
                foreach (var student in students)
                {
                    worksheet.Cells[row, 1].Value = student.EnrollmentNo;
                    worksheet.Cells[row, 2].Value = student.Name;
                    worksheet.Cells[row, 3].Value = student.Class?.Name;
                    worksheet.Cells[row, 4].Value = student.Division?.Name;
                    worksheet.Cells[row, 5].Value = string.Join(",", student.StudentSubjects
                        .Select(ss => ss.Subject.Code));
                    row++;
                }

                // Add validation info in a new sheet
                var validationSheet = package.Workbook.Worksheets.Add("SubjectCodes");
                var subjects = await _context.Subjects.ToListAsync();
                row = 1;
                foreach (var subject in subjects)
                {
                    validationSheet.Cells[row, 1].Value = subject.Code;
                    validationSheet.Cells[row, 2].Value = subject.Name;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                
                var content = package.GetAsByteArray();
                string fileName = $"StudentSubjects_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportSubjects(SubjectMappingViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                TempData["Error"] = "Please select a file to import";
                return RedirectToAction(nameof(SubjectsViaExcel));
            }

            string tempPath = "";
            try
            {
                var previewModel = await ValidateAndPreviewSubjectMapping(model.File);
                
                var fileId = Guid.NewGuid().ToString();
                tempPath = Path.Combine(Path.GetTempPath(), fileId + ".xlsx");
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }
                
                previewModel.FileId = fileId;
                return View("SubjectsPreview", previewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subject mapping preview");
                if (!string.IsNullOrEmpty(tempPath) && System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete temporary file");
                    }
                }
                
                TempData["Error"] = $"Error previewing import: {ex.Message}";
                return RedirectToAction(nameof(SubjectsViaExcel));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSubjectMapping(string fileId)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), fileId + ".xlsx");
            
            try
            {
                if (!System.IO.File.Exists(tempPath))
                {
                    TempData["Error"] = "Import file not found. Please try again.";
                    return RedirectToAction(nameof(SubjectsViaExcel));
                }

                using var package = new ExcelPackage(new FileInfo(tempPath));
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet?.Dimension == null)
                    throw new Exception("The Excel file is empty or invalid");

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var subjectsCache = await _context.Subjects
                        .ToDictionaryAsync(s => s.Code.ToLower());

                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {
                        var enrollmentNo = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(enrollmentNo)) continue;

                        var student = await _context.Students
                            .Include(s => s.StudentSubjects)
                            .FirstOrDefaultAsync(s => s.EnrollmentNo == enrollmentNo);

                        if (student == null) continue;

                        var subjectCodes = (worksheet.Cells[row, 5].Value?.ToString() ?? "")
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToList();

                        // Remove existing mappings
                        _context.StudentSubjects.RemoveRange(student.StudentSubjects);

                        // Add new mappings
                        foreach (var code in subjectCodes)
                        {
                            if (subjectsCache.TryGetValue(code, out var subject))
                            {
                                await _context.StudentSubjects.AddAsync(new StudentSubject
                                {
                                    StudentId = student.Id,
                                    SubjectId = subject.Id
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["Success"] = "Subject mappings updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error updating subject mappings: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subject mapping confirmation");
                TempData["Error"] = $"Import failed: {ex.Message}";
                return RedirectToAction(nameof(SubjectsViaExcel));
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file");
                    }
                }
            }
        }

        private async Task<SubjectMappingPreviewModel> ValidateAndPreviewSubjectMapping(IFormFile file)
        {
            var preview = new SubjectMappingPreviewModel();
            
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            
            if (worksheet?.Dimension == null)
                throw new Exception("The Excel file is empty or invalid");

            preview.TotalRows = worksheet.Dimension.Rows - 1; // Excluding header

            var subjectsCache = await _context.Subjects.ToDictionaryAsync(s => s.Code.ToLower());
            var studentsCache = await _context.Students
                .Include(s => s.StudentSubjects)
                .Include(s => s.Class)
                .Include(s => s.Division)
                .ToDictionaryAsync(s => s.EnrollmentNo.ToLower());

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var mappingRow = new SubjectMappingRow { RowNumber = row };

                try
                {
                    mappingRow.EnrollmentNo = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                    mappingRow.StudentName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                    mappingRow.Class = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                    mappingRow.Division = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                    
                    var subjectCodes = (worksheet.Cells[row, 5].Value?.ToString() ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

                    mappingRow.SubjectCodes = subjectCodes;

                    if (string.IsNullOrEmpty(mappingRow.EnrollmentNo))
                    {
                        mappingRow.Status = MappingStatus.Invalid;
                        mappingRow.StatusMessage = "Enrollment number is required";
                        continue;
                    }

                    if (!studentsCache.TryGetValue(mappingRow.EnrollmentNo.ToLower(), out var student))
                    {
                        mappingRow.Status = MappingStatus.Invalid;
                        mappingRow.StatusMessage = "Student not found";
                        continue;
                    }

                    // Validate subject codes
                    var invalidCodes = subjectCodes
                        .Where(code => !subjectsCache.ContainsKey(code.ToLower()))
                        .ToList();

                    if (invalidCodes.Any())
                    {
                        mappingRow.Status = MappingStatus.Invalid;
                        mappingRow.StatusMessage = $"Invalid subject codes: {string.Join(", ", invalidCodes)}";
                        continue;
                    }

                    var existingSubjectIds = student.StudentSubjects
                        .Select(ss => ss.SubjectId)
                        .ToList();

                    var newSubjectIds = subjectCodes
                        .Select(code => subjectsCache[code.ToLower()].Id)
                        .ToList();

                    if (!existingSubjectIds.Any() && newSubjectIds.Any())
                    {
                        mappingRow.Status = MappingStatus.Valid;
                        preview.NewMappings++;
                    }
                    else if (!existingSubjectIds.SequenceEqual(newSubjectIds))
                    {
                        mappingRow.Status = MappingStatus.Valid;
                        preview.UpdatedMappings++;
                    }
                    else
                    {
                        mappingRow.Status = MappingStatus.NoChange;
                        mappingRow.StatusMessage = "No changes needed";
                    }
                }
                catch (Exception ex)
                {
                    mappingRow.Status = MappingStatus.Invalid;
                    mappingRow.StatusMessage = ex.Message;
                    preview.ValidationErrors.Add($"Row {row}: {ex.Message}");
                }

                preview.Rows.Add(mappingRow);
            }

            return preview;
        }
    }
}