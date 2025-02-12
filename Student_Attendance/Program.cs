using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeOpenXml; // Add this line


var builder = WebApplication.CreateBuilder(args);

// Change your logging configuration to this:
builder.Logging.ClearProviders(); // Clear first
builder.Logging.AddConsole()     // Then add providers
    .AddDebug()
    .SetMinimumLevel(LogLevel.Debug); // Set minimum level to see more details


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddDbContext<Student_Attendance.Data.ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        context.Database.Migrate();

        // Check if any users exist
        if (!context.Users.Any())
        {
            // Create default Admin user
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin"), // Default password: Admin@123
                Role = "Admin"
            };
            context.Users.Add(adminUser);

            // Create default Teacher user
            var teacherUser = new User
            {
                UserName = "teacher",
                Email = "teacher@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("teacher"), // Default password: Teacher@123
                Role = "Teacher"
            };
            context.Users.Add(teacherUser);

            // Create default Student user
            var studentUser = new User
            {
                UserName = "student",
                Email = "student@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("student"), // Default password: Student@123
                Role = "Student"
            };
            context.Users.Add(studentUser);

            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

app.Run();