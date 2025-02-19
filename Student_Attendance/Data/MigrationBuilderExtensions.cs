using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Linq;

namespace Student_Attendance.Data
{
    public static class MigrationBuilderExtensions
    {
        public static bool GetColumnIfExists(this MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            try
            {
                migrationBuilder.Sql($"SELECT {columnName} FROM {tableName} WHERE 1=0");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
