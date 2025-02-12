using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAcademicYearFromSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AcademicYears_AcademicYearId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_AcademicYearId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Subjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_AcademicYearId",
                table: "Subjects",
                column: "AcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AcademicYears_AcademicYearId",
                table: "Subjects",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id");
        }
    }
}
