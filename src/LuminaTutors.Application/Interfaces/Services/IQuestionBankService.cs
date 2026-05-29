using LuminaTutors.Application.DTOs.QuestionBank;
using LuminaTutors.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IQuestionBankService
{
    // ── CRUD ──────────────────────────────────────────────────────────────────
    Task<Result<PagedResult<QuestionListItemDto>>> GetQuestionsAsync(int schoolId, QuestionFilterRequest filter, CancellationToken ct = default);
    Task<Result<QuestionDto>>                      GetByIdAsync(int schoolId, int questionId, CancellationToken ct = default);
    Task<Result<QuestionDto>>                      CreateAsync(int schoolId, int teacherId, CreateQuestionRequest req, CancellationToken ct = default);
    Task<Result<QuestionDto>>                      UpdateAsync(int schoolId, int questionId, UpdateQuestionRequest req, CancellationToken ct = default);
    Task<Result>                                   DeleteAsync(int schoolId, int questionId, CancellationToken ct = default);
    Task<Result>                                   ApproveAsync(int schoolId, int questionId, CancellationToken ct = default);

    // ── Import from URL ───────────────────────────────────────────────────────
    /// <summary>
    /// Fetches the URL, parses question-like content with heuristics, returns a
    /// preview of parsed questions without saving.
    /// </summary>
    Task<Result<ImportPreviewDto>> PreviewUrlImportAsync(int schoolId, int teacherId, ImportFromUrlRequest req, CancellationToken ct = default);

    /// <summary>
    /// Saves an import job, scrapes the URL and persists parsed questions.
    /// </summary>
    Task<Result<ImportJobDto>> ExecuteUrlImportAsync(int schoolId, int teacherId, ImportFromUrlRequest req, CancellationToken ct = default);

    // ── Import from file ──────────────────────────────────────────────────────
    /// <summary>Import từ Excel (.xlsx) — mỗi hàng là một câu hỏi.</summary>
    Task<Result<int>> ImportFromExcelAsync(int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default);

    /// <summary>Import từ Word (.docx) — parse theo định dạng chuẩn.</summary>
    Task<Result<int>> ImportFromWordAsync(int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default);

    /// <summary>Import từ PDF — extract text rồi parse câu hỏi.</summary>
    Task<Result<int>> ImportFromPdfAsync(int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default);

    // ── Import jobs ───────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<ImportJobDto>>> GetImportJobsAsync(int schoolId, CancellationToken ct = default);

    // ── Helpers ───────────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<SubjectOptionDto>>> GetSubjectsAsync(CancellationToken ct = default);

    // ── Stats ─────────────────────────────────────────────────────────────────
    Task<Result<QuestionBankStatsDto>> GetStatsAsync(int schoolId, CancellationToken ct = default);
}
