using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Student_Attendance.Services;
using Student_Attendance.Services.Logging;
using Student_Attendance.ViewModels;
using System.Security.Claims;
using BCrypt.Net;

namespace Student_Attendance.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly ILoggingService _loggingService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger,
            UserManager<User> userManager, SignInManager<User> signInManager, ILoggingService loggingService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _loggingService = loggingService;
            _signInManager = signInManager;
        }

[HttpGet]
[AllowAnonymous] // Ensure this attribute is recognized

        public IActionResult Login(string? returnUrl = null)  // Add ? after string
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)  // Add ? after string
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        var user = await _userManager.FindByNameAsync(model.UserName);
                        await _loggingService.LogActivityAsync(
                            action: "Login",
                            entityType: "User",
                            entityId: user.Id.ToString(),
                            details: $"User {model.UserName} logged in successfully",
                            module: "Authentication",
                            isSuccess: true
                        );
                        return LocalRedirect(returnUrl ?? "/");
                    }
                    else
                    {
                        await _loggingService.LogActivityAsync(
                            action: "Login",
                            entityType: "User",
                            details: $"Failed login attempt for user {model.UserName}",
                            module: "Authentication",
                            isSuccess: false,
                            status: "Failed"
                        );
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(
                    errorMessage: ex.Message,
                    stackTrace: ex.StackTrace,
                    errorType: ex.GetType().Name,
                    source: "AccountController.Login"
                );
                ModelState.AddModelError(string.Empty, "An error occurred during login.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login", "Account");  // Redirect to login page instead of home
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

private IActionResult RedirectToLocal(string? returnUrl) // Add ? to handle null

        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
