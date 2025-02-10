using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Student_Attendance.Data;
using Student_Attendance.ViewModels;
using Student_Attendance.Models;

namespace Student_Attendance.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Attendance/Take
        public async Task<IActionResult> Take()
        {
            var model = new AttendanceViewModel
            {
                Date = DateTime.Today,
                Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name"),
                Divisions = new SelectList(await _context.Divisions.ToListAsync(), "Id", "Name")
            };
            return View(model);
        }

        // POST: Attendance/MarkAttendance
        [HttpPost]
        public async Task<IActionResult> MarkAttendance(AttendanceViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data" });

            try
            {
                foreach (var student in model.Students)
                {
                    var attendance = new AttendanceRecord
                    {
                        StudentId = student.StudentId,
                        SubjectId = model.SubjectId,
                        Date = model.Date,
                        IsPresent = student.IsPresent,
                        AbsenceReason = student.AbsenceReason,
                        TimeStamp = DateTime.Now,
                        MarkedById = User.Identity.Name
                    };
                    _context.AttendanceRecords.Add(attendance);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attendance marked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                return Json(new { success = false, message = "Error marking attendance" });
            }
        }
    }
}
