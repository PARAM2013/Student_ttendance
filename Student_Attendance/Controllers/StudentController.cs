using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Controllers;
using OfficeOpenXml;
using Student_Attendance.ViewModels;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentAttendance.Controllers
{
    public class StudentsController : BaseController
    {
        public StudentsController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students.Include(c => c.Course).Include(c => c.AcademicYear).Include(c => c.Division).ToListAsync();
            return View(students);
        }


        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            StudentViewModel model = new StudentViewModel();
            await LoadDropDowns(model);
            return View(model);
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                Student student = new Student
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
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await LoadDropDowns(model);
            return View(model);
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

            StudentViewModel studentViewModel = new StudentViewModel
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
            await LoadDropDowns(studentViewModel);

            return View(studentViewModel);
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
                    Student student = new Student
                    {
                        Id = model.Id,
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
                    _context.Update(student);
                    await _context.SaveChangesAsync();
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
                return RedirectToAction(nameof(Index));
            }
            await LoadDropDowns(model);
            return View(model);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.Include(s => s.Course).Include(s => s.AcademicYear).Include(s => s.Division)
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
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Import
        public IActionResult Import()
        {
            return View();
        }


        // POST: Students/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.ErrorMessage = "Please select file.";
                return View();
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ErrorMessage = "Please select excel file.";
                return View();
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                if (worksheet.Cells[row, 1].Value == null) continue; //skip empty rows

                                Student student = new Student
                                {
                                    EnrollmentNo = worksheet.Cells[row, 1].Value.ToString().Trim(),
                                    Name = worksheet.Cells[row, 2].Value.ToString().Trim(),
                                    Cast = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                    Email = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                    Mobile = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                    CourseId = Convert.ToInt32(worksheet.Cells[row, 6].Value),
                                    Semester = Convert.ToInt32(worksheet.Cells[row, 7].Value),
                                    IsActive = Convert.ToBoolean(worksheet.Cells[row, 8].Value),
                                    AcademicYearId = Convert.ToInt32(worksheet.Cells[row, 9].Value),
                                    DivisionId = Convert.ToInt32(worksheet.Cells[row, 10].Value)

                                };

                                _context.Students.Add(student);
                            }
                            catch (Exception ex)
                            {
                                //log exception in logging
                                ViewBag.ErrorMessage = "Error in file reading, please correct data in file.";
                                return View();
                            }


                        }
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }

                }
            }
            catch (Exception ex)
            {
                //Log exception in logging
                ViewBag.ErrorMessage = "Error in file reading, please correct data in file.";
                return View();
            }

        }
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
        private async Task LoadDropDowns(StudentViewModel model)
        {
            var courses = await _context.Courses.ToListAsync();
            model.Courses = courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            var academicYears = await _context.AcademicYears.ToListAsync();
            model.AcademicYears = academicYears.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            var divisions = await _context.Divisions.Include(c => c.Class).Include(c => c.Class.Course).Include(c => c.Class.AcademicYear).ToListAsync();
            model.Divisions = divisions.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name + " (" + c.Class.Name + "-" + c.Class.Course.Name + "-" + c.Class.AcademicYear.Name + ")" }).ToList();
        }
    }
}