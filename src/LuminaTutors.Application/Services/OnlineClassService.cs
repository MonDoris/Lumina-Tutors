using LuminaTutors.Application.DTOs.Online;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class OnlineClassService : IOnlineClassService
{
    private readonly IUnitOfWork                 _uow;
    private readonly ILogger<OnlineClassService> _log;

    public OnlineClassService(IUnitOfWork uow, ILogger<OnlineClassService> log)
    {
        _uow = uow;
        _log = log;
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<OnlineSessionDto>>> GetSessionsAsync(
        int schoolId, int userId, string roleCode, CancellationToken ct = default)
    {
        var isTeacher = roleCode is "TEACHER" or "ADMIN";

        var query = _uow.OnlineSessions
            .AsQueryable()
            .Include(s => s.Teacher)
            .Include(s => s.Participants)
            .Where(s => s.SchoolId == schoolId);

        if (isTeacher)
            query = query.Where(s => s.TeacherId == userId);
        else
            query = query.Where(s => s.Status == OnlineSessionStatus.Live
                                  || s.Status == OnlineSessionStatus.Scheduled);

        var sessions = await query
            .OrderByDescending(s => s.ScheduledAt ?? s.CreatedAt)
            .ToListAsync(ct);

        var dtos = sessions.Select(ToDto).ToList() as IReadOnlyList<OnlineSessionDto>;
        return Result<IReadOnlyList<OnlineSessionDto>>.Success(dtos);
    }

    public async Task<Result<OnlineSessionDto>> GetByIdAsync(
        int schoolId, int sessionId, CancellationToken ct = default)
    {
        var session = await _uow.OnlineSessions
            .AsQueryable()
            .Include(s => s.Teacher)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.Id == sessionId, ct);

        return session is null
            ? Result<OnlineSessionDto>.Failure("Phòng học không tồn tại.")
            : Result<OnlineSessionDto>.Success(ToDto(session));
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Result<OnlineSessionDto>> CreateAsync(
        int schoolId, int teacherId, CreateOnlineSessionRequest request,
        CancellationToken ct = default)
    {
        var session = new OnlineSession
        {
            SchoolId        = schoolId,
            TeacherId       = teacherId,
            Title           = request.Title.Trim(),
            Description     = request.Description?.Trim(),
            RoomCode        = GenerateRoomCode(),
            Status          = OnlineSessionStatus.Scheduled,
            ScheduledAt     = request.ScheduledAt?.ToUniversalTime(),
            MaxParticipants = Math.Max(2, request.MaxParticipants)
        };

        await _uow.OnlineSessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, session.Id, ct);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task<Result> StartAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await FindOwned(schoolId, sessionId, teacherId, ct);
        if (s is null) return Result.Failure("Không tìm thấy phòng học.");
        if (s.Status == OnlineSessionStatus.Live) return Result.Success();

        s.Status    = OnlineSessionStatus.Live;
        s.StartedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> EndAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await FindOwned(schoolId, sessionId, teacherId, ct);
        if (s is null) return Result.Failure("Không tìm thấy phòng học.");

        s.Status  = OnlineSessionStatus.Ended;
        s.EndedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await FindOwned(schoolId, sessionId, teacherId, ct);
        if (s is null) return Result.Failure("Không tìm thấy phòng học.");
        if (s.Status == OnlineSessionStatus.Live)
            return Result.Failure("Không thể hủy phòng đang phát. Vui lòng kết thúc trước.");

        s.Status = OnlineSessionStatus.Cancelled;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await FindOwned(schoolId, sessionId, teacherId, ct);
        if (s is null) return Result.Failure("Không tìm thấy phòng học.");
        if (s.Status == OnlineSessionStatus.Live)
            return Result.Failure("Không thể xóa phòng đang phát. Vui lòng kết thúc trước.");

        _uow.OnlineSessions.Remove(s);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Join / Leave ──────────────────────────────────────────────────────────

    public async Task<Result<JoinSessionResult>> JoinAsync(
        int schoolId, int sessionId, int userId, CancellationToken ct = default)
    {
        var session = await _uow.OnlineSessions
            .AsQueryable()
            .Include(s => s.Teacher)
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.Id == sessionId, ct);

        if (session is null)
            return Result<JoinSessionResult>.Failure("Phòng học không tồn tại.");

        if (session.Status == OnlineSessionStatus.Ended
         || session.Status == OnlineSessionStatus.Cancelled)
            return Result<JoinSessionResult>.Failure("Phòng học đã kết thúc hoặc bị hủy.");

        // Record participant (upsert: only one active entry per user per session)
        var existing = await _uow.SessionParticipants
            .AsQueryable()
            .Where(p => p.SessionId == sessionId && p.UserId == userId && p.LeftAt == null)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            await _uow.SessionParticipants.AddAsync(new SessionParticipant
            {
                SessionId = sessionId,
                UserId    = userId,
                JoinedAt  = DateTime.UtcNow
            }, ct);
            await _uow.SaveChangesAsync(ct);
        }

        var jitsiRoom = $"lumina-{schoolId}-{session.RoomCode}".ToLowerInvariant();

        return Result<JoinSessionResult>.Success(new JoinSessionResult(
            SessionId:   session.Id,
            RoomCode:    session.RoomCode,
            JitsiRoom:   jitsiRoom,
            Title:       session.Title,
            TeacherName: session.Teacher.FullName,
            Status:      session.Status.ToString(),
            SchoolId:    schoolId
        ));
    }

    public async Task<Result> LeaveAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        var p = await _uow.SessionParticipants
            .AsQueryable()
            .Where(x => x.SessionId == sessionId && x.UserId == userId && x.LeftAt == null)
            .FirstOrDefaultAsync(ct);

        if (p is not null)
        {
            p.LeftAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    // ── Participants ──────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<OnlineSessionParticipantDto>>> GetParticipantsAsync(
        int sessionId, CancellationToken ct = default)
    {
        var list = await _uow.SessionParticipants
            .AsQueryable()
            .Include(p => p.User).ThenInclude(u => u.Role)
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.JoinedAt)
            .Select(p => new OnlineSessionParticipantDto(
                p.UserId,
                p.User.FullName,
                p.User.Role.RoleName,
                p.JoinedAt,
                p.LeftAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<OnlineSessionParticipantDto>>.Success(
            list as IReadOnlyList<OnlineSessionParticipantDto>);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<OnlineSession?> FindOwned(
        int schoolId, int sessionId, int teacherId, CancellationToken ct)
        => await _uow.OnlineSessions
            .AsQueryable()
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId
                                   && s.Id        == sessionId
                                   && s.TeacherId == teacherId, ct);

    private static OnlineSessionDto ToDto(OnlineSession s) => new(
        SessionId:        s.Id,
        SchoolId:         s.SchoolId,
        TeacherId:        s.TeacherId,
        TeacherName:      s.Teacher?.FullName ?? "",
        Title:            s.Title,
        Description:      s.Description,
        RoomCode:         s.RoomCode,
        Status:           s.Status.ToString(),
        ScheduledAt:      s.ScheduledAt,
        StartedAt:        s.StartedAt,
        EndedAt:          s.EndedAt,
        MaxParticipants:  s.MaxParticipants,
        ParticipantCount: s.Participants?.Select(p => p.UserId).Distinct().Count() ?? 0,
        CreatedAt:        s.CreatedAt
    );

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rand = Random.Shared;
        var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[rand.Next(chars.Length)]).ToArray());
        var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[rand.Next(chars.Length)]).ToArray());
        return $"{part1}-{part2}";
    }
}
