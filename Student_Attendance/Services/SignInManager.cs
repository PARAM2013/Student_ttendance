using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Student_Attendance.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

namespace Student_Attendance.Services
{
    public class SignInManager<TUser> where TUser : User
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<SignInManager<TUser>> _logger;

        public SignInManager(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor, ILogger<SignInManager<TUser>> logger)
        {
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                return SignInResult.Failed;
            }

            await SignInAsync(user, isPersistent);
            return SignInResult.Success;
        }

        public async Task SignInAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await _contextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }

    public class SignInResult
    {
        public bool Succeeded { get; private set; }

        private SignInResult(bool succeeded)
        {
            Succeeded = succeeded;
        }

        public static SignInResult Success => new SignInResult(true);
        public static SignInResult Failed => new SignInResult(false);
    }
}