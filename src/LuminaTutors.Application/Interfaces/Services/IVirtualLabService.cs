using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Manages 3D virtual lab sessions — creation, lookup, and closure.
/// </summary>
public interface IVirtualLabService
{
    /// <summary>Returns all currently active sessions for a school.</summary>
    Task<Result<List<LabSessionDto>>> GetActiveSessionsAsync(int schoolId, CancellationToken ct = default);

    /// <summary>Teacher opens a new lab session; returns the session with its join code.</summary>
    Task<Result<LabSessionDto>> CreateSessionAsync(int schoolId, int teacherId, CreateLabSessionRequest req, CancellationToken ct = default);

    /// <summary>Find a session by its 6-char code (for students joining).</summary>
    Task<Result<LabSessionDto>> GetByCodeAsync(int schoolId, string code, CancellationToken ct = default);

    /// <summary>Find a session by its numeric ID (for entering the lab).</summary>
    Task<Result<LabSessionDto>> GetByIdAsync(int schoolId, int sessionId, CancellationToken ct = default);

    /// <summary>Host teacher closes their session; marks it inactive.</summary>
    Task<Result> CloseSessionAsync(int schoolId, int sessionId, int teacherId, CancellationToken ct = default);
}
