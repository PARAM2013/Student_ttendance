using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;
using System.Linq;
using System.Threading.Tasks;

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
                    DivisionId = model.DivisionId
                };

                try
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Student created successfully!" });
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
    }
}