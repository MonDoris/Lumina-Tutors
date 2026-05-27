using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVirtualLabSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualLabSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionCode = table.Column<string>(type: "nchar(6)", fixedLength: true, maxLength: 6, nullable: false),
                    SubjectTag = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SceneType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 40),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualLabSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_VirtualLabSessions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "SchoolId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirtualLabSessions_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualLabSessions_School_Code",
                table: "VirtualLabSessions",
                columns: new[] { "SchoolId", "SessionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualLabSessions_TeacherId",
                table: "VirtualLabSessions",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualLabSessions");
        }
    }
}
