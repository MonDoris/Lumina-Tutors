using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionRecordings",
                columns: table => new
                {
                    RecordingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OnlineSessionId = table.Column<int>(type: "int", nullable: true),
                    RoomLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    TeacherName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRecordings", x => x.RecordingId);
                    table.ForeignKey(
                        name: "FK_SessionRecordings_OnlineSessions_OnlineSessionId",
                        column: x => x.OnlineSessionId,
                        principalTable: "OnlineSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SessionRecordings_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "SchoolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordings_OnlineSessionId",
                table: "SessionRecordings",
                column: "OnlineSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordings_School",
                table: "SessionRecordings",
                column: "SchoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionRecordings");
        }
    }
}
