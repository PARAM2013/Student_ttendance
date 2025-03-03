using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIsActiveField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Courses");
        }
    }
}
