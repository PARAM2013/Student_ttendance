using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollmentHistories_AcademicYearId",
                table: "StudentEnrollmentHistories",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollmentHistories_ClassId",
                table: "StudentEnrollmentHistories",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollmentHistories_CourseId",
                table: "StudentEnrollmentHistories",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollmentHistories_DivisionId",
                table: "StudentEnrollmentHistories",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendanceArchives_AcademicYearId",
                table: "StudentAttendanceArchives",
                column: "AcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAttendanceArchives_AcademicYears_AcademicYearId",
                table: "StudentAttendanceArchives",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollmentHistories_AcademicYears_AcademicYearId",
                table: "StudentEnrollmentHistories",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollmentHistories_Classes_ClassId",
                table: "StudentEnrollmentHistories",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollmentHistories_Courses_CourseId",
                table: "StudentEnrollmentHistories",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollmentHistories_Divisions_DivisionId",
                table: "StudentEnrollmentHistories",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAttendanceArchives_AcademicYears_AcademicYearId",
                table: "StudentAttendanceArchives");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollmentHistories_AcademicYears_AcademicYearId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollmentHistories_Classes_ClassId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollmentHistories_Courses_CourseId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollmentHistories_Divisions_DivisionId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollmentHistories_AcademicYearId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollmentHistories_ClassId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollmentHistories_CourseId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollmentHistories_DivisionId",
                table: "StudentEnrollmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_StudentAttendanceArchives_AcademicYearId",
                table: "StudentAttendanceArchives");
        }
    }
}
