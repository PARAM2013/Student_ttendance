using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddDicusstion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscussionTopic",
                table: "AttendanceRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscussionTopic",
                table: "AttendanceRecords");
        }
    }
}
