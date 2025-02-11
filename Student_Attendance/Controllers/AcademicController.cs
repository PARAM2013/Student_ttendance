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
        public async Task<IActionResult> CreateAcademicYear(AcademicYear model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _context.AcademicYears.AddAsync(model);
                    var result = await _context.SaveChangesAsync();

                    if (result > 0)
                    {
                        return Json(new { success = true, message = "Academic Year created successfully" });
                    }
                    return Json(new { success = false, message = "Failed to save Academic Year" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, message = "Validation failed", errors = errors });
        }


        [HttpGet]
        public IActionResult EditAcademicYear(int id)
        {
            var academicYear = _context.AcademicYears.Find(id);
            if (academicYear == null)
            {
                return Json(new { success = false, message = "Academic Year not found" });
            }
            return PartialView("_AddEditAcademicYear", academicYear);
        }

        [HttpPost]
        public async Task<IActionResult> EditAcademicYear(AcademicYear model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.AcademicYears.Update(model);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Academic Year updated successfully" });
                }

                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAcademicYear(int id)
        {
            try
            {
                var academicYear = await _context.AcademicYears.FindAsync(id);
                if (academicYear == null)
                {
                    return Json(new { success = false, message = "Academic Year not found" });
                }

                _context.AcademicYears.Remove(academicYear);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Academic Year deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
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
        public async Task<IActionResult> CreateCourse(Course model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _context.Courses.AddAsync(model);
                    var result = await _context.SaveChangesAsync();
                    
                    if (result > 0)
                    {
                        return Json(new { success = true, message = "Course created successfully" });
                    }
                    return Json(new { success = false, message = "Failed to save course" });
                }

                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult EditCourse(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
            {
                return Json(new { success = false, message = "Course not found" });
            }
            return PartialView("_AddEditCourse", course);
        }

        [HttpPost]
        public async Task<IActionResult> EditCourse(Course model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var course = await _context.Courses.FindAsync(model.Id);
                    if (course == null)
                    {
                        return Json(new { success = false, message = "Course not found" });
                    }

                    course.Name = model.Name;
                    _context.Courses.Update(course);
                    var result = await _context.SaveChangesAsync();

                    if (result > 0)
                    {
                        return Json(new { success = true, message = "Course updated successfully" });
                    }
                    return Json(new { success = false, message = "Failed to update course" });
                }

                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Course deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
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

        // Specializations part code

        public IActionResult Specializations()
        {
            var specializations = _context.Specializations.Include(s => s.Course).ToList();
            return View(specializations);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSpecialization()
        {
            SpecializationViewModel model = new SpecializationViewModel();
            await LoadSpecializationDropDowns(model);
            return PartialView("_AddEditSpecialization", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSpecialization(int id)
        {
            var specialization = await _context.Specializations.FindAsync(id);
            if (specialization == null)
            {
                return NotFound();
            }

            SpecializationViewModel model = new SpecializationViewModel
            {
                Id = specialization.Id,
                Name = specialization.Name,
                CourseId = specialization.CourseId
            };
            await LoadSpecializationDropDowns(model);
            return PartialView("_AddEditSpecialization", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSpecialization(SpecializationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadSpecializationDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                Specialization specialization = new Specialization
                {
                    Name = model.Name,
                    CourseId = model.CourseId
                };

                await _context.Specializations.AddAsync(specialization);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Specialization created successfully" });
                }
                return Json(new { success = false, message = "Failed to save specialization" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditSpecialization(SpecializationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadSpecializationDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                var specialization = await _context.Specializations.FindAsync(model.Id);
                if (specialization == null)
                {
                    return Json(new { success = false, message = "Specialization not found" });
                }

                specialization.Name = model.Name;
                specialization.CourseId = model.CourseId;

                _context.Specializations.Update(specialization);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Specialization updated successfully" });
                }
                return Json(new { success = false, message = "Failed to update specialization" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialization(int id)
        {
            try
            {
                var specialization = await _context.Specializations.FindAsync(id);
                if (specialization == null)
                {
                    return Json(new { success = false, message = "Specialization not found" });
                }

                _context.Specializations.Remove(specialization);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Specialization deleted successfully" });
                }
                return Json(new { success = false, message = "Failed to delete specialization" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task LoadSpecializationDropDowns(SpecializationViewModel model)
        {
            var courses = await _context.Courses.ToListAsync();
            model.Courses = courses.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
        }

        // Add Subjects code

        // Add these actions to your AcademicController class

        public IActionResult Subjects()
        {
            var subjects = _context.Subjects
                .Include(s => s.Course)
                .Include(s => s.AcademicYear)
                .Include(s => s.Specialization)
                .ToList();
            return View(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSubject()
        {
            SubjectViewModel model = new SubjectViewModel();
            await LoadSubjectDropDowns(model);
            return PartialView("_AddEditSubject", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            var model = new SubjectViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                SpecializationId = subject.SpecializationId,
                Semester = subject.Semester,
                CourseId = subject.CourseId,
                AcademicYearId = subject.AcademicYearId,
                ClassId = subject.ClassId
            };

            await LoadSubjectDropDowns(model);
            return PartialView("_AddEditSubject", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(SubjectViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadSubjectDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                var subject = new Subject
                {
                    Name = model.Name,
                    Code = model.Code,
                    SpecializationId = model.SpecializationId,
                    Semester = model.Semester,
                    CourseId = model.CourseId,
                    AcademicYearId = model.AcademicYearId,
                    ClassId = model.ClassId
                };

                await _context.Subjects.AddAsync(subject);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Subject created successfully" });
                }
                return Json(new { success = false, message = "Failed to save subject" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditSubject(SubjectViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadSubjectDropDowns(model);
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                var subject = await _context.Subjects.FindAsync(model.Id);
                if (subject == null)
                {
                    return Json(new { success = false, message = "Subject not found" });
                }

                subject.Name = model.Name;
                subject.Code = model.Code;
                subject.SpecializationId = model.SpecializationId;
                subject.Semester = model.Semester;
                subject.CourseId = model.CourseId;
                subject.AcademicYearId = model.AcademicYearId;
                subject.ClassId = model.ClassId;

                _context.Subjects.Update(subject);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Subject updated successfully" });
                }
                return Json(new { success = false, message = "Failed to update subject" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            try
            {
                var subject = await _context.Subjects.FindAsync(id);
                if (subject == null)
                {
                    return Json(new { success = false, message = "Subject not found" });
                }

                _context.Subjects.Remove(subject);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new { success = true, message = "Subject deleted successfully" });
                }
                return Json(new { success = false, message = "Failed to delete subject" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task LoadSubjectDropDowns(SubjectViewModel model)
        {
            if (model.Courses == null) model.Courses = new List<SelectListItem>();
            if (model.AcademicYears == null) model.AcademicYears = new List<SelectListItem>();
            if (model.Specializations == null) model.Specializations = new List<SelectListItem>();
            if (model.Classes == null) model.Classes = new List<SelectListItem>();

            var courses = await _context.Courses.ToListAsync();
            var academicYears = await _context.AcademicYears.ToListAsync();
            var specializations = await _context.Specializations.ToListAsync();
            var classes = await _context.Classes.ToListAsync();

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

            // Add an empty option for Specialization since it's optional
            var specializationItems = specializations.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();
            model.Specializations = specializationItems;

            model.Classes = classes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
        }

    }
}