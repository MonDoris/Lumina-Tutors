using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowFKsAndEnumDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_SubjectAssignments_SubjectAssignmentId1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_SubjectAssignments_SubjectAssignmentId1",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_SubjectAssignmentId1",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_SubjectAssignmentId1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SubjectAssignmentId1",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SubjectAssignmentId1",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClassEnrollments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "SessionStatus",
                table: "AttendanceSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Open");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Absent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectAssignmentId1",
                table: "Lessons",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClassEnrollments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "SessionStatus",
                table: "AttendanceSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Absent",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "SubjectAssignmentId1",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectAssignmentId1",
                table: "Lessons",
                column: "SubjectAssignmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SubjectAssignmentId1",
                table: "Assignments",
                column: "SubjectAssignmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_SubjectAssignments_SubjectAssignmentId1",
                table: "Assignments",
                column: "SubjectAssignmentId1",
                principalTable: "SubjectAssignments",
                principalColumn: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_SubjectAssignments_SubjectAssignmentId1",
                table: "Lessons",
                column: "SubjectAssignmentId1",
                principalTable: "SubjectAssignments",
                principalColumn: "AssignmentId");
        }
    }
}
