using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;


namespace Student_Attendance.Controllers
{
    public class AcademicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AcademicController> _logger;

        public AcademicController(ApplicationDbContext context, ILogger<AcademicController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult AcademicYears()
        {
            var academicYears = _context.AcademicYears.ToList();
            return View(academicYears);
        }


        [HttpGet]
        public IActionResult CreateAcademicYear()
        {
            return PartialView("_AddEditAcademicYear", new AcademicYear());
        }


        [HttpPost]
        public IActionResult CreateAcademicYear(AcademicYear model)
        {
            if (ModelState.IsValid)
            {
                _context.AcademicYears.Add(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }
            return PartialView("_AddEditAcademicYear", model);
        }


        [HttpGet]
        public IActionResult EditAcademicYear(int id)
        {
            var academicYear = _context.AcademicYears.Find(id);
            if (academicYear == null)
            {
                return NotFound();
            }
            return PartialView("_AddEditAcademicYear", academicYear);
        }

        [HttpPost]
        public IActionResult EditAcademicYear(AcademicYear model)
        {
            if (ModelState.IsValid)
            {
                _context.AcademicYears.Update(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }
            return PartialView("_AddEditAcademicYear", model);
        }

        [HttpPost]
        public IActionResult DeleteAcademicYear(int id)
        {
            var academicYear = _context.AcademicYears.Find(id);
            if (academicYear == null)
            {
                return NotFound();
            }

            try
            {
                _context.AcademicYears.Remove(academicYear);
                _context.SaveChanges();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);

            }

        }
        public IActionResult Courses()
        {
            var courses = _context.Courses.ToList();
            return View(courses);
        }

        [HttpGet]
        public IActionResult CreateCourse()
        {
            return PartialView("_AddEditCourse", new Course());
        }


        [HttpPost]
        public IActionResult CreateCourse(Course model)
        {
            if (ModelState.IsValid)
            {
                _context.Courses.Add(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }

            return PartialView("_AddEditCourse", model);
        }


        [HttpGet]
        public IActionResult EditCourse(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }
            return PartialView("_AddEditCourse", course);
        }

        [HttpPost]
        public IActionResult EditCourse(Course model)
        {
            if (ModelState.IsValid)
            {
                _context.Courses.Update(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }
            return PartialView("_AddEditCourse", model);
        }

        [HttpPost]
        public IActionResult DeleteCourse(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }
            try
            {
                _context.Courses.Remove(course);
                _context.SaveChanges();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);

            }
        }

        public IActionResult Classes()
        {
            var classes = _context.Classes.Include(c => c.Course)
                                        .Include(c => c.AcademicYear)
                                        .ToList();
            return View(classes);
        }

        [HttpGet]
        public async Task<IActionResult> CreateClass()
        {
            var model = new ClassViewModel();
            await LoadClassDropDowns(model);
            return PartialView("_AddEditClass", model);
        }



        [HttpPost]
        public async Task<IActionResult> CreateClass(ClassViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadClassDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                // Verify the values are present
                if (model.CourseId <= 0 || model.AcademicYearId <= 0)
                {
                    return Json(new { success = false, message = "Invalid Course or Academic Year selection" });
                }

                var classObj = new Class
                {
                    Name = model.Name,
                    CourseId = model.CourseId,
                    AcademicYearId = model.AcademicYearId
                };

                // Verify the referenced entities exist
                var courseExists = await _context.Courses.AnyAsync(c => c.Id == model.CourseId);
                var academicYearExists = await _context.AcademicYears.AnyAsync(a => a.Id == model.AcademicYearId);

                if (!courseExists || !academicYearExists)
                {
                    return Json(new { success = false, message = "Selected Course or Academic Year does not exist" });
                }

                await _context.Classes.AddAsync(classObj);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Class created successfully" });
                }

                return Json(new { success = false, message = "Failed to save to database" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



        private async Task LoadClassDropDowns(ClassViewModel model)
        {
            var courses = await _context.Courses.ToListAsync();
            var academicYears = await _context.AcademicYears.ToListAsync();

            model.Courses = courses.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            model.AcademicYears = academicYears.Select(ay => new SelectListItem
            {
                Value = ay.Id.ToString(),
                Text = ay.Name
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> EditClass(int id)
        {
            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null)
            {
                return NotFound();
            }

            ClassViewModel model = new ClassViewModel
            {
                Id = classObj.Id,
                Name = classObj.Name,
                CourseId = classObj.CourseId,
                AcademicYearId = classObj.AcademicYearId
            };
            await LoadClassDropDowns(model);
            return PartialView("_AddEditClass", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditClass(ClassViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var existingClass = await _context.Classes.FindAsync(model.Id);
                if (existingClass == null)
                {
                    return Json(new { success = false, message = "Class not found" });
                }

                // Update existing class properties
                existingClass.Name = model.Name;
                existingClass.CourseId = model.CourseId;
                existingClass.AcademicYearId = model.AcademicYearId;

                _context.Classes.Update(existingClass);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Class updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating class: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null)
            {
                return NotFound();
            }

            try
            {
                _context.Classes.Remove(classObj);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // division part code

        public IActionResult Divisions()
        {
            var divisions = _context.Divisions
                                   .Include(d => d.Class)
                                   .ThenInclude(c => c.Course)
                                   .ToList();
            return View(divisions);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDivision()
        {
            DivisionViewModel model = new DivisionViewModel();
            await LoadDivisionDropDowns(model);
            return PartialView("_AddEditDivision", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDivision(DivisionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDivisionDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                Division division = new Division
                {
                    Name = model.Name,
                    ClassId = model.ClassId
                };

                await _context.Divisions.AddAsync(division);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Division created successfully" });
                }
                return Json(new { success = false, message = "Failed to save division" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditDivision(int id)
        {
            var division = await _context.Divisions.FindAsync(id);
            if (division == null)
            {
                return NotFound();
            }

            DivisionViewModel model = new DivisionViewModel
            {
                Id = division.Id,
                Name = division.Name,
                ClassId = division.ClassId
            };
            await LoadDivisionDropDowns(model);
            return PartialView("_AddEditDivision", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditDivision(DivisionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDivisionDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                var division = new Division
                {
                    Id = model.Id,
                    Name = model.Name,
                    ClassId = model.ClassId
                };

                _context.Divisions.Update(division);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Division updated successfully" });
                }
                return Json(new { success = false, message = "Failed to update division" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDivision(int id)
        {
            var division = await _context.Divisions.FindAsync(id);
            if (division == null)
            {
                return Json(new { success = false, message = "Division not found" });
            }

            try
            {
                _context.Divisions.Remove(division);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Division deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting division: {ex.Message}" });
            }
        }

        private async Task LoadDivisionDropDowns(DivisionViewModel model)
        {
            try
            {
                var classes = await _context.Classes
                    .Include(c => c.Course)
                    .Include(c => c.AcademicYear)
                    .ToListAsync();

                model.Classes = classes.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} ({c.Course.Name} - {c.AcademicYear.Name})"
                }).ToList();

                if (!model.Classes.Any())
                {
                    throw new Exception("No classes found in database");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading classes: {ex.Message}");
            }
        }



    }
}