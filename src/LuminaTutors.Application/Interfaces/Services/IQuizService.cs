using LuminaTutors.Application.DTOs.Quiz;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IQuizService
{
    // ── Question Bank ─────────────────────────────────────────────────────────
    Task<Result<PagedResult<QuizQuestionDto>>> GetQuestionsAsync(
        int schoolId, QuizQuestionFilterRequest filter, int page, int pageSize,
        CancellationToken ct = default);

    Task<Result<QuizQuestionDto>> GetQuestionByIdAsync(
        int schoolId, int questionId,
        CancellationToken ct = default);

    Task<Result<QuizQuestionDto>> CreateQuestionAsync(
        int schoolId, int teacherId, CreateQuestionRequest request,
        CancellationToken ct = default);

    Task<Result<QuizQuestionDto>> UpdateQuestionAsync(
        int schoolId, int questionId, UpdateQuestionRequest request,
        CancellationToken ct = default);

    Task<Result> DeleteQuestionAsync(
        int schoolId, int questionId,
        CancellationToken ct = default);

    Task<Result> ApproveQuestionAsync(
        int schoolId, int questionId,
        CancellationToken ct = default);

    // ── Quiz Exam ─────────────────────────────────────────────────────────────
    Task<Result<PagedResult<QuizExamDto>>> GetExamsAsync(
        int schoolId, int? createdByUserId, string? status, int page, int pageSize,
        CancellationToken ct = default);

    Task<Result<QuizExamDto>> GetExamByIdAsync(
        int schoolId, int examId,
        CancellationToken ct = default);

    Task<Result<QuizExamDto>> CreateExamAsync(
        int schoolId, int teacherId, CreateQuizExamRequest request,
        CancellationToken ct = default);

    Task<Result> PublishExamAsync(
        int schoolId, int examId,
        CancellationToken ct = default);

    Task<Result> CloseExamAsync(
        int schoolId, int examId,
        CancellationToken ct = default);

    Task<Result> DeleteExamAsync(
        int schoolId, int examId,
        CancellationToken ct = default);

    // ── Student Attempts ──────────────────────────────────────────────────────
    Task<Result<StudentAttemptDto>> StartAttemptAsync(
        int schoolId, int studentId, int examId,
        CancellationToken ct = default);

    Task<Result<StudentAttemptDto>> GetMyAttemptAsync(
        int studentId, int examId,
        CancellationToken ct = default);

    Task<Result> SaveAnswerAsync(
        int attemptId, int studentId, SaveAnswerRequest request,
        CancellationToken ct = default);

    Task<Result<AttemptResultDto>> SubmitAttemptAsync(
        int attemptId, int studentId,
        CancellationToken ct = default);

    Task<Result<PagedResult<ExamResultSummaryDto>>> GetExamResultsAsync(
        int schoolId, int examId, int page, int pageSize,
        CancellationToken ct = default);

    Task<Result<AttemptResultDto>> GetAttemptResultAsync(
        int attemptId, int requestingUserId,
        CancellationToken ct = default);
}
