using Microsoft.EntityFrameworkCore;
using Student_Attendance.Data;
using Student_Attendance.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Check if any users exist
        if (!context.Users.Any())
        {
            // Create default Admin user
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                Password = "admin123",  // Password should be hashed in production
                Role = "Admin"
            };
            context.Users.Add(adminUser);

            // Create default Teacher user
            var teacherUser = new User
            {
                UserName = "teacher",
                Email = "teacher@example.com",
                Password = "teacher123",  // Password should be hashed in production
                Role = "Teacher"
            };
            context.Users.Add(teacherUser);


            context.SaveChanges();
        }
        // Check if any Academic Year exist
        try
        {
            if (!context.AcademicYears.Any())
            {
                // Create default Academic Year
                var academicYear = new AcademicYear
                {
                    Name = "2023-2024",
                    StartDate = new DateTime(2023, 7, 1),
                    EndDate = new DateTime(2024, 6, 30),
                    IsActive = true
                };
                context.AcademicYears.Add(academicYear);


                // Create default Academic Year
                var academicYear2 = new AcademicYear
                {
                    Name = "2024-2025",
                    StartDate = new DateTime(2024, 7, 1),
                    EndDate = new DateTime(2025, 6, 30),
                    IsActive = false
                };
                context.AcademicYears.Add(academicYear2);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Academic Year.");
        }
        // Check if any Courses exist
        try
        {
            if (!context.Courses.Any())
            {
                // Create default Course
                var course = new Course
                {
                    Name = "MBA"
                };
                context.Courses.Add(course);
                var course1 = new Course
                {
                    Name = "MCA"
                };
                context.Courses.Add(course1);

                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Courses.");
        }
        // Check if any Class exist
        try
        {
            if (!context.Classes.Any())
            {
                var course = context.Courses.FirstOrDefault(c => c.Name == "MBA");
                var academicYear = context.AcademicYears.FirstOrDefault(a => a.Name == "2023-2024");
                if (course == null)
                {
                    // Create default Course
                    course = new Course
                    {
                        Name = "MBA"
                    };
                    context.Courses.Add(course);
                    context.SaveChanges();
                }
                if (academicYear == null)
                {
                    // Create default Academic Year
                    academicYear = new AcademicYear
                    {
                        Name = "2023-2024",
                        StartDate = new DateTime(2023, 7, 1),
                        EndDate = new DateTime(2024, 6, 30),
                        IsActive = true
                    };
                    context.AcademicYears.Add(academicYear);
                    context.SaveChanges();
                }
                // Create default Class
                var class1 = new Class
                {
                    Name = "MBA Class A",
                    CourseId = course.Id,
                    AcademicYearId = academicYear.Id

                };
                context.Classes.Add(class1);
                var class2 = new Class
                {
                    Name = "MBA Class B",
                    CourseId = course.Id,
                    AcademicYearId = academicYear.Id
                };
                context.Classes.Add(class2);
                var course1 = context.Courses.FirstOrDefault(c => c.Name == "MCA");
                if (course1 == null)
                {
                    // Create default Course
                    course1 = new Course
                    {
                        Name = "MCA"
                    };
                    context.Courses.Add(course1);
                    context.SaveChanges();
                }
                // Create default Class
                var class3 = new Class
                {
                    Name = "MCA Class A",
                    CourseId = course1.Id,
                    AcademicYearId = academicYear.Id

                };
                context.Classes.Add(class3);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Classes.");
        }
        // Check if any Division exist
        try
        {
            if (!context.Divisions.Any())
            {
                var class1 = context.Classes.FirstOrDefault(c => c.Name == "MBA Class A");
                var class2 = context.Classes.FirstOrDefault(c => c.Name == "MBA Class B");
                var class3 = context.Classes.FirstOrDefault(c => c.Name == "MCA Class A");
                // Create default Division
                var division = new Division
                {
                    Name = "Division A",
                    ClassId = class1.Id
                };
                context.Divisions.Add(division);
                var division1 = new Division
                {
                    Name = "Division B",
                    ClassId = class1.Id
                };
                context.Divisions.Add(division1);
                var division2 = new Division
                {
                    Name = "Division C",
                    ClassId = class2.Id
                };
                context.Divisions.Add(division2);
                var division3 = new Division
                {
                    Name = "Division A",
                    ClassId = class3.Id
                };
                context.Divisions.Add(division3);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Divisions.");
        }
        // Check if any Specializations exist
        try
        {
            if (!context.Specializations.Any())
            {
                var course = context.Courses.FirstOrDefault(c => c.Name == "MBA");
                var specialization1 = new Specialization
                {
                    Name = "Finance",
                    CourseId = course.Id

                };
                context.Specializations.Add(specialization1);
                var specialization2 = new Specialization
                {
                    Name = "Marketing",
                    CourseId = course.Id

                };
                context.Specializations.Add(specialization2);
                var course1 = context.Courses.FirstOrDefault(c => c.Name == "MCA");
                var specialization3 = new Specialization
                {
                    Name = "Software Development",
                    CourseId = course1.Id
                };
                context.Specializations.Add(specialization3);
                context.SaveChanges();
            }

        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Specializations.");
        }
        // Check if any Subjects exist
        try
        {
            if (!context.Subjects.Any())
            {
                var specialization1 = context.Specializations.FirstOrDefault(s => s.Name == "Finance");
                var specialization2 = context.Specializations.FirstOrDefault(s => s.Name == "Marketing");
                var specialization3 = context.Specializations.FirstOrDefault(s => s.Name == "Software Development");
                var academicYear = context.AcademicYears.FirstOrDefault(a => a.Name == "2023-2024");
                var course = context.Courses.FirstOrDefault(c => c.Name == "MBA");
                var course1 = context.Courses.FirstOrDefault(c => c.Name == "MCA");
                var subject1 = new Subject
                {
                    Name = "Financial Accounting",
                    Code = "MBA-101",
                    Semester = 1,
                    AcademicYearId = academicYear.Id,
                    CourseId = course.Id
                };
                context.Subjects.Add(subject1);
                var subject2 = new Subject
                {
                    Name = "Business Communication",
                    Code = "MBA-102",
                    Semester = 1,
                    AcademicYearId = academicYear.Id,
                    CourseId = course.Id
                };
                context.Subjects.Add(subject2);
                var subject3 = new Subject
                {
                    Name = "Consumer Behavior",
                    Code = "MBA-301",
                    SpecializationId = specialization2.Id,
                    Semester = 3,
                    AcademicYearId = academicYear.Id,
                    CourseId = course.Id
                };
                context.Subjects.Add(subject3);
                var subject4 = new Subject
                {
                    Name = "Database Management System",
                    Code = "MCA-101",
                    Semester = 1,
                    AcademicYearId = academicYear.Id,
                    CourseId = course1.Id
                };
                context.Subjects.Add(subject4);
                var subject5 = new Subject
                {
                    Name = "Software Engineering",
                    Code = "MCA-301",
                    SpecializationId = specialization3.Id,
                    Semester = 3,
                    AcademicYearId = academicYear.Id,
                    CourseId = course1.Id
                };
                context.Subjects.Add(subject5);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB Subjects.");
        }

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }

}
app.Run();