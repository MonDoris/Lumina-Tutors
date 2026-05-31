using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineClassroomFull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SessionParticipants: add missing attendance columns ───────────────
            migrationBuilder.AddColumn<bool>(
                name: "IsAttended",
                table: "SessionParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "SessionParticipants",
                type: "datetime2",
                nullable: true);

            // ── QuestionBanks: add missing columns ────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "QuestionBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "QuestionBanks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "QuestionBanks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // ── OnlineRoomChats ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "OnlineRoomChats",
                columns: table => new
                {
                    ChatId      = table.Column<int>(type: "int", nullable: false)
                                      .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId   = table.Column<int>(type: "int", nullable: false),
                    SenderId    = table.Column<int>(type: "int", nullable: false),
                    Content     = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Text"),
                    SentAt      = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineRoomChats", x => x.ChatId);
                    table.ForeignKey(
                        name: "FK_OnlineRoomChats_OnlineSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "OnlineSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnlineRoomChats_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineRoomChats_SessionId_SentAt",
                table: "OnlineRoomChats",
                columns: new[] { "SessionId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineRoomChats_SenderId",
                table: "OnlineRoomChats",
                column: "SenderId");

            // ── OnlineSlides ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "OnlineSlides",
                columns: table => new
                {
                    SlideId    = table.Column<int>(type: "int", nullable: false)
                                     .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId  = table.Column<int>(type: "int", nullable: false),
                    FileName   = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileUrl    = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalPages = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineSlides", x => x.SlideId);
                    table.ForeignKey(
                        name: "FK_OnlineSlides_OnlineSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "OnlineSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineSlides_SessionId",
                table: "OnlineSlides",
                column: "SessionId");

            // ── QuestionImportJobs ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "QuestionImportJobs",
                columns: table => new
                {
                    ImportJobId       = table.Column<int>(type: "int", nullable: false)
                                            .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId          = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    TargetSubjectId   = table.Column<int>(type: "int", nullable: false),
                    SourceUrl         = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status            = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ImportedCount     = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage      = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt       = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionImportJobs", x => x.ImportJobId);
                    table.ForeignKey(
                        name: "FK_QuestionImportJobs_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "SchoolId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionImportJobs_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionImportJobs_Subjects_TargetSubjectId",
                        column: x => x.TargetSubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionImportJobs_SchoolId",
                table: "QuestionImportJobs",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionImportJobs_RequestedByUserId",
                table: "QuestionImportJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionImportJobs_TargetSubjectId",
                table: "QuestionImportJobs",
                column: "TargetSubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QuestionImportJobs");
            migrationBuilder.DropTable(name: "OnlineSlides");
            migrationBuilder.DropTable(name: "OnlineRoomChats");

            migrationBuilder.DropColumn(name: "Tags",          table: "QuestionBanks");
            migrationBuilder.DropColumn(name: "SourceUrl",     table: "QuestionBanks");
            migrationBuilder.DropColumn(name: "CorrectAnswer", table: "QuestionBanks");
            migrationBuilder.DropColumn(name: "AttendedAt",    table: "SessionParticipants");
            migrationBuilder.DropColumn(name: "IsAttended",    table: "SessionParticipants");
        }
    }
}
