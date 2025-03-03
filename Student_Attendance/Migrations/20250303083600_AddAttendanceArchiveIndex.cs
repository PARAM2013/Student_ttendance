using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceArchiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendanceArchives_StudentId_Date_SubjectId",
                table: "StudentAttendanceArchives",
                columns: new[] { "StudentId", "Date", "SubjectId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAttendanceArchives_Students_StudentId",
                table: "StudentAttendanceArchives",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAttendanceArchives_Students_StudentId",
                table: "StudentAttendanceArchives");

            migrationBuilder.DropIndex(
                name: "IX_StudentAttendanceArchives_StudentId_Date_SubjectId",
                table: "StudentAttendanceArchives");
        }
    }
}
