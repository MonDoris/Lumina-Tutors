using LuminaTutors.Application.DTOs.AI;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IAiTutorService
{
    // ── Student ────────────────────────────────────────────────────────────────

    Task<Result<AiTutorSessionDto>> CreateSessionAsync(
        int schoolId, int studentId, string title, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AiTutorSessionDto>>> GetStudentSessionsAsync(
        int schoolId, int studentId, CancellationToken ct = default);

    Task<Result<AiTutorSessionDto>> GetSessionAsync(
        int schoolId, int sessionId, int requestingUserId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AiTutorMessageDto>>> GetMessagesAsync(
        int sessionId, CancellationToken ct = default);

    /// <summary>Gửi tin nhắn của học sinh và trả về phản hồi từ AI.</summary>
    Task<Result<AiTutorMessageDto>> SendMessageAsync(
        int schoolId, int sessionId, int studentId, string content, CancellationToken ct = default);

    Task<Result> DeleteSessionAsync(
        int schoolId, int sessionId, int studentId, CancellationToken ct = default);

    // ── Admin ──────────────────────────────────────────────────────────────────

    Task<Result<IReadOnlyList<AiTutorSessionDto>>> GetAllSessionsAsync(
        int schoolId, bool? flaggedOnly = null, CancellationToken ct = default);

    Task<Result> UpdateAdminNoteAsync(
        int sessionId, string? note, bool isFlagged, CancellationToken ct = default);

    Task<Result> FlagMessageAsync(
        int messageId, bool isFlagged, CancellationToken ct = default);

    Task<Result> MarkSessionReviewedAsync(
        int sessionId, CancellationToken ct = default);
}
