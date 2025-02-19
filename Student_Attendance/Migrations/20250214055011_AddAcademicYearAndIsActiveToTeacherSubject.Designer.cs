using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Student_Attendance.Data;

#nullable disable

namespace Student_Attendance.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250214055011_AddAcademicYearAndIsActiveToTeacherSubject")]
    partial class AddAcademicYearAndIsActiveToTeacherSubject
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            // ...existing model configuration...
        }
    }
}
