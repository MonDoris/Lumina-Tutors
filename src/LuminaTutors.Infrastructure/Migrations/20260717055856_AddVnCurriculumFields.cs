using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVnCurriculumFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "SemesterNo",
                table: "CourseModules",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "StartWeek",
                table: "CourseModules",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CognitiveLevel",
                table: "CourseLessons",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Objectives",
                table: "CourseLessons",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PeriodCount",
                table: "CourseLessons",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SemesterNo",
                table: "CourseModules");

            migrationBuilder.DropColumn(
                name: "StartWeek",
                table: "CourseModules");

            migrationBuilder.DropColumn(
                name: "CognitiveLevel",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "Objectives",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "PeriodCount",
                table: "CourseLessons");
        }
    }
}
