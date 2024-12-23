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
                return RedirectToAction("AcademicYears");
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
                return RedirectToAction("AcademicYears");
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
            _context.AcademicYears.Remove(academicYear);
            _context.SaveChanges();
            return RedirectToAction("AcademicYears");
        }

        //Courses page code

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
                return RedirectToAction("Courses");
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
                return RedirectToAction("Courses");
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
            _context.Courses.Remove(course);
            _context.SaveChanges();
            return RedirectToAction("Courses");
        }

        //Classes page 
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
        public IActionResult CreateClass(Class model)
        {
            // Debug information
            var modelStateErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            if (!ModelState.IsValid)
            {
                // Log the errors
                foreach (var error in modelStateErrors)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation Error: {error}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Classes.Add(model);
                    _context.SaveChanges();
                    return RedirectToAction("Classes");
                }
                catch (Exception ex)
                {
                    // Log the exception
                    System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
                    ModelState.AddModelError("", "Error saving to database");
                }
            }

            ViewBag.Courses = _context.Courses.ToList();
            ViewBag.AcademicYears = _context.AcademicYears.ToList();
            return PartialView("_AddEditClass", model);
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
                _context.Classes.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Classes");
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
            _context.Classes.Remove(classItem);
            _context.SaveChanges();
            return RedirectToAction("Classes");
        }

        // page 

        // page 
    }
}