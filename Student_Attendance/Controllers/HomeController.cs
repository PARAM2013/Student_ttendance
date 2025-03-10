using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Student_Attendance.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Search_Attendance()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchStudents(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
            {
                return Json(new List<object>());
            }

            var students = await _context.Students
                .Include(s => s.Class) // Make sure Class is included
                .Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) // Case-insensitive search
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    className = s.Class != null ? s.Class.Name : "No Class" // Handle null Class
                })
                .Take(10)
                .ToListAsync();

            return Json(students);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentAttendance(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Get all attendance records for the student
                var attendanceRecords = await _context.AttendanceRecords
                    .Include(a => a.Subject)
                    .Where(a => a.StudentId == studentId)
                    .ToListAsync();

                // Calculate overall attendance
                int totalClasses = attendanceRecords.Count(); 
                int attendedClasses = attendanceRecords.Count(a => a.IsPresent);
                double overallAttendance = totalClasses > 0 
                    ? (double)attendedClasses / totalClasses * 100 
                    : 0;

                // Get the last attendance date
                var lastAttendanceDate = attendanceRecords.Any() 
                    ? attendanceRecords.Max(a => a.Date).ToString("MMMM dd, yyyy") 
                    : null;

                // Calculate subject-wise attendance
                var subjectAttendance = attendanceRecords
                    .GroupBy(a => a.Subject)
                    .Select(g => new
                    {
                        subjectName = g.Key.Name,
                        total = g.Count(),
                        present = g.Count(a => a.IsPresent)
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    studentName = student.Name,
                    className = student.Class.Name,
                    totalClasses = totalClasses,
                    attendedClasses = attendedClasses,
                    overallAttendance = overallAttendance,
                    subjectAttendance = subjectAttendance,
                    lastAttendanceDate = lastAttendanceDate
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}