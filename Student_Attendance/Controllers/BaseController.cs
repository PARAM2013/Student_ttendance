using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;

namespace Student_Attendance.Controllers
{
    public class BaseController : Controller
    {
        protected ApplicationDbContext _context;
        public User CurrentUser { get; set; }

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            string username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = _context.Users.FirstOrDefault(u => u.UserName == username);
            }
            else
            {
                CurrentUser = null;
            }
        }
    }
}