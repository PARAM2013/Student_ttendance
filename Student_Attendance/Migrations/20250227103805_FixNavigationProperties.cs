using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class FixNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectId1",
                table: "StudentSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubjects_SubjectId1",
                table: "StudentSubjects",
                column: "SubjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId1",
                table: "StudentSubjects",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubjects_Subjects_SubjectId1",
                table: "StudentSubjects");

            migrationBuilder.DropIndex(
                name: "IX_StudentSubjects_SubjectId1",
                table: "StudentSubjects");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "StudentSubjects");
        }
    }
}
