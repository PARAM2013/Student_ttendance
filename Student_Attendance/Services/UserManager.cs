using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using System.Threading.Tasks;

namespace Student_Attendance.Services
{
    public class UserManager<TUser> where TUser : User
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManager<TUser>> _logger;

        public UserManager(ApplicationDbContext context, ILogger<UserManager<TUser>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> FindByNameAsync(string userName)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            if (user == null)
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.Password);
        }
    }
}