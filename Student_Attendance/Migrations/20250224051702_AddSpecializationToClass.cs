using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendance.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecializationToClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassId1",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Classes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SpecializationId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassId1",
                table: "Students",
                column: "ClassId1");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SpecializationId",
                table: "Classes",
                column: "SpecializationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Specializations_SpecializationId",
                table: "Classes",
                column: "SpecializationId",
                principalTable: "Specializations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_ClassId1",
                table: "Students",
                column: "ClassId1",
                principalTable: "Classes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Specializations_SpecializationId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_ClassId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ClassId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Classes_SpecializationId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ClassId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "Classes");
        }
    }
}
