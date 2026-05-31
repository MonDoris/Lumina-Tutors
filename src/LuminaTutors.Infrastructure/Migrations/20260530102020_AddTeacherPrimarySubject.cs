using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherPrimarySubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OnlineRoomChats_SessionId_SentAt",
                table: "OnlineRoomChats");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionImportJobs_SchoolId",
                table: "QuestionImportJobs",
                newName: "IX_QuestionImportJobs_School");

            migrationBuilder.RenameIndex(
                name: "IX_OnlineSlides_SessionId",
                table: "OnlineSlides",
                newName: "IX_OnlineSlides_Session");

            migrationBuilder.AddColumn<int>(
                name: "PrimarySubjectId",
                table: "TeacherProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAttended",
                table: "SessionParticipants",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "QuestionImportJobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "QuestionImportJobs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "ImportedCount",
                table: "QuestionImportJobs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "QuestionBanks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "QuestionBanks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "OnlineSlides",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "OnlineSlides",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(260)",
                oldMaxLength: 260);

            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "OnlineRoomChats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Text");

            migrationBuilder.CreateTable(
                name: "AssignmentAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSizeKB = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentAttachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_AssignmentAttachments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfiles_PrimarySubjectId",
                table: "TeacherProfiles",
                column: "PrimarySubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineRoomChats_Session",
                table: "OnlineRoomChats",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttachments_AssignmentId",
                table: "AssignmentAttachments",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfiles_Subjects_PrimarySubjectId",
                table: "TeacherProfiles",
                column: "PrimarySubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_Subjects_PrimarySubjectId",
                table: "TeacherProfiles");

            migrationBuilder.DropTable(
                name: "AssignmentAttachments");

            migrationBuilder.DropIndex(
                name: "IX_TeacherProfiles_PrimarySubjectId",
                table: "TeacherProfiles");

            migrationBuilder.DropIndex(
                name: "IX_OnlineRoomChats_Session",
                table: "OnlineRoomChats");

            migrationBuilder.DropColumn(
                name: "PrimarySubjectId",
                table: "TeacherProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionImportJobs_School",
                table: "QuestionImportJobs",
                newName: "IX_QuestionImportJobs_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_OnlineSlides_Session",
                table: "OnlineSlides",
                newName: "IX_OnlineSlides_SessionId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAttended",
                table: "SessionParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "QuestionImportJobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "QuestionImportJobs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<int>(
                name: "ImportedCount",
                table: "QuestionImportJobs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "QuestionBanks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "QuestionBanks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "OnlineSlides",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "OnlineSlides",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "OnlineRoomChats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Text",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineRoomChats_SessionId_SentAt",
                table: "OnlineRoomChats",
                columns: new[] { "SessionId", "SentAt" });
        }
    }
}
