using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicYearAndIsActiveToTeacherSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "TeacherSubjects",
                type: "int",
                nullable: false,
                defaultValue: 1);  // Default to 1 or another appropriate value

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TeacherSubjects",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_AcademicYearId",
                table: "TeacherSubjects",
                column: "AcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_AcademicYears_AcademicYearId",
                table: "TeacherSubjects",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_AcademicYears_AcademicYearId",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_AcademicYearId",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TeacherSubjects");
        }
    }
}
