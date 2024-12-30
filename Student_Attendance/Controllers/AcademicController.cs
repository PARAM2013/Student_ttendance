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
            if (ModelState.IsValid)
            {
                try
                {
                    Class classObj = new Class
                    {
                        Name = model.Name,
                        CourseId = model.CourseId,
                        AcademicYearId = model.AcademicYearId
                    };

                    await _context.Classes.AddAsync(classObj);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Class created successfully" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            await LoadClassDropDowns(model);
            return Json(new { success = false, message = "Validation failed" });
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
                
    }
}