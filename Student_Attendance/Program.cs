using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeOpenXml;
using Student_Attendance.Services;
using Student_Attendance.Services.Logging;
using Microsoft.AspNetCore.Http;

using CustomUserManager = Student_Attendance.Services.UserManager<Student_Attendance.Models.User>;
using CustomSignInManager = Student_Attendance.Services.SignInManager<Student_Attendance.Models.User>;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole()
    .AddDebug()
    .SetMinimumLevel(LogLevel.Debug);

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

builder.Services.AddScoped<SSIDGenerator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILoggingService, LoggingService>();

// Add custom User management services
builder.Services.AddScoped<CustomUserManager>(provider => 
{
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var logger = provider.GetRequiredService<ILogger<CustomUserManager>>();
    return new CustomUserManager(context, logger);
});

builder.Services.AddScoped<CustomSignInManager>(provider => 
{
    var userManager = provider.GetRequiredService<CustomUserManager>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var logger = provider.GetRequiredService<ILogger<CustomSignInManager>>();
    return new CustomSignInManager(userManager, httpContextAccessor, logger);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapControllerRoute(
    name: "getClassesByCourse",
    pattern: "StudentCarryForward/GetClassesByCourse",
    defaults: new { controller = "StudentCarryForward", action = "GetClassesByCourse" }
);

app.MapControllerRoute(
    name: "getDivisionsByClass",
    pattern: "StudentCarryForward/GetDivisionsByClass/{classId}",
    defaults: new { controller = "StudentCarryForward", action = "GetDivisionsByClass" }
);

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
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = "Admin"
            };
            context.Users.Add(adminUser);

            var teacherUser = new User
            {
                UserName = "teacher",
                Email = "teacher@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("teacher"),
                Role = "Teacher"
            };
            context.Users.Add(teacherUser);

            var studentUser = new User
            {
                UserName = "student",
                Email = "student@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                Role = "Student"
            };
            context.Users.Add(studentUser);

            context.SaveChanges();
        }

        if (!context.Courses.Any())
        {
            var course = new Course { Name = "MBA" };
            context.Courses.Add(course);
            context.SaveChanges();
        }
        var dummyCourse = await context.Courses.FirstOrDefaultAsync() 
            ?? throw new InvalidOperationException("No courses found");

        if (!context.AcademicYears.Any())
        {
            var acadYear = new AcademicYear { Name = "2024-2025", StartDate = DateTime.Today.AddMonths(-3), EndDate = DateTime.Today.AddMonths(9), IsActive = true };
            context.AcademicYears.Add(acadYear);
            context.SaveChanges();
        }
        var dummyYear = context.AcademicYears.First();

        if (!context.Classes.Any())
        {
            var dummyClass = new Class { Name = "MBA SEM 1", CourseId = dummyCourse.Id, AcademicYearId = dummyYear.Id };
            context.Classes.Add(dummyClass);
            context.SaveChanges();
        }
        var dummyClassExisting = context.Classes.First();

        if (!context.Divisions.Any())
        {
            var dummyDivision = new Division { Name = "A", ClassId = dummyClassExisting.Id };
            context.Divisions.Add(dummyDivision);
            context.SaveChanges();
        }
        var dummyDivisionExisting = context.Divisions.First();

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
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

app.Run();