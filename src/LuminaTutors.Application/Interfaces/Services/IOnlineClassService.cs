using LuminaTutors.Application.DTOs.Online;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IOnlineClassService
{
    /// <summary>Get sessions the user can see (teacher sees own; student sees Live + Scheduled).</summary>
    Task<Result<IReadOnlyList<OnlineSessionDto>>> GetSessionsAsync(
        int schoolId, int userId, string roleCode,
        CancellationToken ct = default);

    Task<Result<OnlineSessionDto>> GetByIdAsync(
        int schoolId, int sessionId,
        CancellationToken ct = default);

    Task<Result<OnlineSessionDto>> CreateAsync(
        int schoolId, int teacherId, CreateOnlineSessionRequest request,
        CancellationToken ct = default);

    /// <summary>Teacher starts the session (Status → Live).</summary>
    Task<Result> StartAsync(int schoolId, int sessionId, int teacherId,
        CancellationToken ct = default);

    /// <summary>Teacher ends the session (Status → Ended).</summary>
    Task<Result> EndAsync(int schoolId, int sessionId, int teacherId,
        CancellationToken ct = default);

    Task<Result> CancelAsync(int schoolId, int sessionId, int teacherId,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(int schoolId, int sessionId, int teacherId,
        CancellationToken ct = default);

    /// <summary>Record that a user joined — returns the Jitsi room details.</summary>
    Task<Result<JoinSessionResult>> JoinAsync(
        int schoolId, int sessionId, int userId,
        CancellationToken ct = default);

    /// <summary>Record that a user left.</summary>
    Task<Result> LeaveAsync(int sessionId, int userId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<OnlineSessionParticipantDto>>> GetParticipantsAsync(
        int sessionId, CancellationToken ct = default);
}
