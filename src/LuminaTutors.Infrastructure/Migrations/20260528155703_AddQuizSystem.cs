using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizExams",
                columns: table => new
                {
                    QuizExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    GradeLevelId = table.Column<int>(type: "int", nullable: true),
                    CreatedByTeacherId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PointsPerQuestion = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1m),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShuffleQuestions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShuffleOptions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowResultAfter = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizExams", x => x.QuizExamId);
                    table.ForeignKey(
                        name: "FK_QuizExams_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "GradeLevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizExams_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "SchoolId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizExams_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizExams_Users_CreatedByTeacherId",
                        column: x => x.CreatedByTeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuizExamQuestions",
                columns: table => new
                {
                    QuizExamQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizExamQuestions", x => x.QuizExamQuestionId);
                    table.ForeignKey(
                        name: "FK_QuizExamQuestions_QuestionBanks_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBanks",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizExamQuestions_QuizExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "QuizExams",
                        principalColumn: "QuizExamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentQuizAttempts",
                columns: table => new
                {
                    AttemptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExamCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ShuffleSeed = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TotalCorrect = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuizAttempts", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_StudentQuizAttempts_QuizExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "QuizExams",
                        principalColumn: "QuizExamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentQuizAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentQuizAnswers",
                columns: table => new
                {
                    QuizAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuizAnswers", x => x.QuizAnswerId);
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_QuestionBanks_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBanks",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_QuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "OptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentQuizAnswers_StudentQuizAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "StudentQuizAttempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizExamQuestions_QuestionId",
                table: "QuizExamQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "UQ_QuizExamQuestions_Exam_Question",
                table: "QuizExamQuestions",
                columns: new[] { "ExamId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizExams_CreatedByTeacherId",
                table: "QuizExams",
                column: "CreatedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizExams_GradeLevelId",
                table: "QuizExams",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizExams_School_Status",
                table: "QuizExams",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizExams_SubjectId",
                table: "QuizExams",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswers_QuestionId",
                table: "StudentQuizAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAnswers_SelectedOptionId",
                table: "StudentQuizAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "UQ_StudentQuizAnswers_Attempt_Question",
                table: "StudentQuizAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizAttempts_StudentId",
                table: "StudentQuizAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_StudentQuizAttempts_Exam_Student",
                table: "StudentQuizAttempts",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizExamQuestions");

            migrationBuilder.DropTable(
                name: "StudentQuizAnswers");

            migrationBuilder.DropTable(
                name: "StudentQuizAttempts");

            migrationBuilder.DropTable(
                name: "QuizExams");
        }
    }
}
