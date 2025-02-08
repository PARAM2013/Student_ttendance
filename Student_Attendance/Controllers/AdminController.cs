using Microsoft.AspNetCore.Authorization;  // Add this using statement
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.ViewModels;

namespace Student_Attendance.Controllers
{
    [Authorize]              // This ensures only logged-in users can access
    [Authorize(Roles = "Admin")]  // This ensures only Admin role can access
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var model = new UserViewModel 
            {
                Roles = new List<string> { "Admin", "Teacher", "Student" }
            };
            return PartialView("_AddEditUser", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == model.UserName))
                {
                    return Json(new { success = false, message = "Username already exists" });
                }

                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password), // Hash password
                    Role = model.Role
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User created successfully" });
            }

            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Roles = new List<string> { "Admin", "Teacher", "Student" }
            };

            return PartialView("_AddEditUser", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }
                user.Role = model.Role;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully" });
            }

            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "User deleted successfully" });
        }
    }
}