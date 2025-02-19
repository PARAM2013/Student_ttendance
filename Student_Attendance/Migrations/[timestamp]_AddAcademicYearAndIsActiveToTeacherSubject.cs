using Microsoft.EntityFrameworkCore.Migrations;

namespace Student_Attendance.Migrations
{
    public partial class AddAcademicYearAndIsActiveToTeacherSubject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "TeacherSubjects",
                type: "int",
                nullable: false,
                defaultValue: 1);  // Set a default value appropriate for your system

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
