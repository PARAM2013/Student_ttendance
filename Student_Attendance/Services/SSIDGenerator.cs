using Student_Attendance.Data;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Student_Attendance.Services
{
    public class SSIDGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random;

        public SSIDGenerator(ApplicationDbContext context)
        {
            _context = context;
            _random = new Random();
        }

        public async Task<string> GenerateUniqueSSID()
        {
            while (true)
            {
                // Generates a random 6-digit number between 100000 and 999999
                string ssid = _random.Next(100000, 999999).ToString();
                
                // Checks if this SSID already exists in the database
                var exists = await _context.Students.AnyAsync(s => s.SSID == ssid);
                
                // If it doesn't exist, return this SSID
                if (!exists)
                {
                    return ssid;
                }
                // If it exists, generate a new one and try again
            }
        }
    }
}
