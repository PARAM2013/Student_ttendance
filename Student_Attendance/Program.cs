using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeOpenXml; // Add this line
using Student_Attendance.Services; // Add this line at the top with other using statements

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

// Add this line to register SSIDGenerator
builder.Services.AddScoped<SSIDGenerator>();

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
    name: "login",
    pattern: "login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Search_Attendance}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created and up-to-date.
        context.Database.Migrate();

        // Seed default users if they don't exist.
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin"), // Default password: Admin@123
                Role = "Admin"
            };
            context.Users.Add(adminUser);

            var teacherUser = new User
            {
                UserName = "teacher",
                Email = "teacher@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("teacher"), // Default password: Teacher@123
                Role = "Teacher"
            };
            context.Users.Add(teacherUser);

            var studentUser = new User
            {
                UserName = "student",
                Email = "student@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Student@123"), // Default password: Student@123
                Role = "Student"
            };
            context.Users.Add(studentUser);

            context.SaveChanges();
        }

        // Seed dummy related data if not exists.
        if (!context.Courses.Any())
        {
            var course = new Course { Name = "Dummy Course" };
            context.Courses.Add(course);
            context.SaveChanges();
        }
        var dummyCourse = context.Courses.First();

        if (!context.AcademicYears.Any())
        {
            var acadYear = new AcademicYear { Name = "2024-2025", StartDate = DateTime.Today.AddMonths(-3), EndDate = DateTime.Today.AddMonths(9), IsActive = true };
            context.AcademicYears.Add(acadYear);
            context.SaveChanges();
        }
        var dummyYear = context.AcademicYears.First();

        if (!context.Classes.Any())
        {
            var dummyClass = new Class { Name = "Class A", CourseId = dummyCourse.Id, AcademicYearId = dummyYear.Id };
            context.Classes.Add(dummyClass);
            context.SaveChanges();
        }
        var dummyClassExisting = context.Classes.First();

        if (!context.Divisions.Any())
        {
            var dummyDivision = new Division { Name = "Division 1", ClassId = dummyClassExisting.Id };
            context.Divisions.Add(dummyDivision);
            context.SaveChanges();
        }
        var dummyDivisionExisting = context.Divisions.First();

        // Seed dummy Students if not exists.
        if (!context.Students.Any())
        {
            for (int i = 1; i <= 10; i++)
            {
                var student = new Student
                {
                    EnrollmentNo = $"S{i:000}",
                    Name = $"Student {i}",
                    Cast = "Dummy Cast",
                    Email = $"student{i}@example.com",
                    Mobile = "1234567890",
                    CourseId = dummyCourse.Id,
                    Semester = 1,
                    IsActive = true,
                    AcademicYearId = dummyYear.Id,
                    DivisionId = dummyDivisionExisting.Id,
                    ClassId = dummyClassExisting.Id
                };
                context.Students.Add(student);
            }
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