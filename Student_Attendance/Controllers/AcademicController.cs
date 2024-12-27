using Microsoft.AspNetCore.Mvc;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.EntityFrameworkCore;

namespace Student_Attendance.Controllers
{
    public class AcademicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcademicController(ApplicationDbContext context)
        {
            _context = context;
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
            var classes = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToList();
            return View(classes);
        }

        [HttpGet]
        public IActionResult CreateClass()
        {
            ViewBag.Courses = _context.Courses.ToList();
            ViewBag.AcademicYears = _context.AcademicYears.ToList();
            return PartialView("_AddEditClass", new Class());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateClass(Class model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verify the values are present
                    if (model.CourseId == 0 || model.AcademicYearId == 0)
                    {
                        return Json(new { success = false, message = "Please select both Course and Academic Year" });
                    }

                    _context.Classes.Add(model);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Class created successfully" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // If we get here, something failed
            var errors = string.Join(", ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return Json(new { success = false, message = errors });
        }


        [HttpGet]
        public IActionResult EditClass(int id)
        {
            var classItem = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).FirstOrDefault(c => c.Id == id);

            if (classItem == null)
            {
                return NotFound();
            }
            ViewBag.Courses = _context.Courses.ToList();
            ViewBag.AcademicYears = _context.AcademicYears.ToList();
            return PartialView("_AddEditClass", classItem);
        }

        [HttpPost]
        public IActionResult EditClass(Class model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Classes.Update(model);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Class updated successfully" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            ViewBag.Courses = _context.Courses.ToList();
            ViewBag.AcademicYears = _context.AcademicYears.ToList();
            return PartialView("_AddEditClass", model);
        }

        [HttpPost]
        public IActionResult DeleteClass(int id)
        {
            var classItem = _context.Classes.Find(id);
            if (classItem == null)
            {
                return NotFound();
            }

            try
            {
                _context.Classes.Remove(classItem);
                _context.SaveChanges();
                return Ok("Class deleted successfully");
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public IActionResult Divisions()
        {
            var divisions = _context.Divisions
                 .Include(d => d.Class)
                     .ThenInclude(c => c.Course)
                  .Include(d => d.Class)
                     .ThenInclude(c => c.AcademicYear)
                 .ToList();

            return View(divisions);
        }

        [HttpGet]
        public IActionResult CreateDivision()
        {
            ViewBag.Classes = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToList();
            return PartialView("_AddEditDivision", new Division());
        }


        [HttpPost]
        public IActionResult CreateDivision(Division model)
        {
            if (ModelState.IsValid)
            {
                _context.Divisions.Add(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }
            ViewBag.Classes = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToList();
            return PartialView("_AddEditDivision", model);
        }


        [HttpGet]
        public IActionResult EditDivision(int id)
        {
            var divisionItem = _context.Divisions.Include(d => d.Class).FirstOrDefault(d => d.Id == id);

            if (divisionItem == null)
            {
                return NotFound();
            }
            ViewBag.Classes = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToList();
            return PartialView("_AddEditDivision", divisionItem);
        }

        [HttpPost]
        public IActionResult EditDivision(Division model)
        {
            if (ModelState.IsValid)
            {
                _context.Divisions.Update(model);
                _context.SaveChanges();
                return Ok(); // Return a 200 OK status code
            }
            ViewBag.Classes = _context.Classes.Include(c => c.Course).Include(c => c.AcademicYear).ToList();
            return PartialView("_AddEditDivision", model);
        }

        [HttpPost]
        public IActionResult DeleteDivision(int id)
        {
            var divisionItem = _context.Divisions.Find(id);
            if (divisionItem == null)
            {
                return NotFound();
            }

            try
            {
                _context.Divisions.Remove(divisionItem);
                _context.SaveChanges();
                return PartialView("_AlertPartial", "Division deleted successfully");
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}