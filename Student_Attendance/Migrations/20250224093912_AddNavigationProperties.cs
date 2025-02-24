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
            migrationBuilder.AddColumn<int>(
                name: "SubjectId1",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId1",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId1",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId1",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_SubjectId1",
                table: "TeacherSubjects",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CourseId1",
                table: "Subjects",
                column: "CourseId1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CourseId1",
                table: "Students",
                column: "CourseId1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId1",
                table: "AttendanceRecords",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId1",
                table: "AttendanceRecords",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Courses_CourseId1",
                table: "Students",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Courses_CourseId1",
                table: "Subjects",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId1",
                table: "TeacherSubjects",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Courses_CourseId1",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Courses_CourseId1",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId1",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_SubjectId1",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_CourseId1",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Students_CourseId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "AttendanceRecords");
        }
    }
}
