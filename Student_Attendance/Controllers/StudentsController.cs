using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

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

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Fetching all students");
                var students = await _context.Students
                    .Include(s => s.Course)
                    .Include(s => s.AcademicYear)
                    .Include(s => s.Division)
                    .Select(s => new StudentViewModel
                    {
                        Id = s.Id,
                        EnrollmentNo = s.EnrollmentNo,
                        Name = s.Name,
                        Course = s.Course,
                        AcademicYear = s.AcademicYear,
                        Division = s.Division,
                        Cast = s.Cast,
                        Email = s.Email,
                        Mobile = s.Mobile,
                        Semester = s.Semester,
                        IsActive = s.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} students", students.Count);
                return View(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching students");
                return Problem("Error retrieving students");
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create Student form");
                StudentViewModel model = new StudentViewModel();
                await LoadStudentDropDowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading Create form");
                return Problem("Error loading create form");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            try
            {
                _logger.LogInformation("Attempting to create student: {@StudentModel}", model);

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

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created student with ID: {StudentId}", student.Id);
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Invalid model state for student creation: {@ModelStateErrors}", 
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                
                await LoadStudentDropDowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating student: {@StudentModel}", model);
                ModelState.AddModelError("", "Unable to create student. Please try again.");
                await LoadStudentDropDowns(model);
                return View(model);
            }
        }

        private async Task LoadStudentDropDowns(StudentViewModel model)
        {
            try
            {
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
                ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Name");
                ViewBag.Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdown data");
                throw;
            }
        }
    }
}