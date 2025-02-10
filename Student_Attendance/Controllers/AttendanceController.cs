using Microsoft.AspNetCore.Mvc;

namespace Student_Attendance.Controllers
{
    public class AttendanceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
