using LuminaTutors.Application.DTOs.OnlineClassroom;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IOnlineClassroomService
{
    // ── Session CRUD ──────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<OnlineSessionListDto>>> GetSessionsAsync(int schoolId, CancellationToken ct = default);
    Task<Result<OnlineSessionDto>>                   GetByIdAsync(int schoolId, int sessionId, CancellationToken ct = default);
    Task<Result<OnlineSessionDto>>                   CreateAsync(int schoolId, int teacherId, CreateOnlineSessionRequest req, CancellationToken ct = default);
    Task<Result<OnlineSessionDto>>                   UpdateAsync(int schoolId, int sessionId, UpdateOnlineSessionRequest req, CancellationToken ct = default);
    Task<Result>                                     DeleteAsync(int schoolId, int sessionId, CancellationToken ct = default);

    // ── Session lifecycle ─────────────────────────────────────────────────────
    Task<Result<OnlineSessionDto>>  StartSessionAsync(int schoolId, int sessionId, int teacherId, CancellationToken ct = default);
    Task<Result<OnlineSessionDto>>  EndSessionAsync(int schoolId, int sessionId, int teacherId, CancellationToken ct = default);
    Task<Result<JoinRoomResult>>    JoinByCodeAsync(int schoolId, int userId, string roomCode, CancellationToken ct = default);

    // ── Participants ──────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<ParticipantDto>>> GetParticipantsAsync(int sessionId, CancellationToken ct = default);
    Task<Result>                                RecordJoinAsync(int sessionId, int userId, CancellationToken ct = default);
    Task<Result>                                RecordLeaveAsync(int sessionId, int userId, CancellationToken ct = default);
    Task<Result>                                MarkAttendanceAsync(int sessionId, int studentUserId, int markedByUserId, CancellationToken ct = default);

    // ── Chat ──────────────────────────────────────────────────────────────────
    Task<Result<ChatMessageDto>>               SaveChatMessageAsync(int sessionId, int senderId, string content, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ChatMessageDto>>> GetChatHistoryAsync(int sessionId, int pageSize = 50, CancellationToken ct = default);

    // ── Slides ────────────────────────────────────────────────────────────────
    Task<Result<OnlineSlideDto>> UploadSlideAsync(int sessionId, string fileName, string fileUrl, int totalPages, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OnlineSlideDto>>> GetSlidesAsync(int sessionId, CancellationToken ct = default);
    Task<Result>                                DeleteSlideAsync(int sessionId, int slideId, CancellationToken ct = default);
}
