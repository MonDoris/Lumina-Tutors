using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class VirtualLabService : IVirtualLabService
{
    private readonly IUnitOfWork                  _uow;
    private readonly ILogger<VirtualLabService>   _logger;

    // Valid subject/scene combinations
    private static readonly HashSet<string> ValidSubjects =
        new(StringComparer.OrdinalIgnoreCase) { "chemistry", "physics", "biology", "math" };

    private static readonly HashSet<string> ValidScenes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Chemistry
            "titration", "electrolysis", "distillation",
            // Physics
            "pendulum", "spring", "optics",
            // Biology
            "cell", "dna", "photosynthesis",
            // Math
            "polyhedron", "vectors"
        };

    public VirtualLabService(IUnitOfWork uow, ILogger<VirtualLabService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ─── GetActiveSessionsAsync ───────────────────────────────────────────────

    public async Task<Result<List<LabSessionDto>>> GetActiveSessionsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var sessions = await _uow.VirtualLabSessions.FindAsync(
            s => s.SchoolId == schoolId && s.IsActive,
            include: q => q.Include(s => s.Teacher),
            ct: ct);

        var dtos = sessions
            .OrderByDescending(s => s.CreatedAt)
            .Select(MapToDto)
            .ToList();

        return Result<List<LabSessionDto>>.Success(dtos);
    }

    // ─── CreateSessionAsync ───────────────────────────────────────────────────

    public async Task<Result<LabSessionDto>> CreateSessionAsync(
        int schoolId, int teacherId, CreateLabSessionRequest req, CancellationToken ct = default)
    {
        if (!ValidSubjects.Contains(req.SubjectTag))
            return Result<LabSessionDto>.Failure("Môn học không hợp lệ. Chọn: chemistry, physics, biology, math.");

        if (!ValidScenes.Contains(req.SceneType))
            return Result<LabSessionDto>.Failure("Loại thí nghiệm không hợp lệ.");

        // Generate unique 6-char code
        string code;
        int    attempts = 0;
        do
        {
            code = GenerateCode();
            attempts++;
            if (attempts > 20)
                return Result<LabSessionDto>.Failure("Không thể tạo mã phòng. Vui lòng thử lại.");
        }
        while (await _uow.VirtualLabSessions.AnyAsync(
            s => s.SchoolId == schoolId && s.SessionCode == code && s.IsActive, ct));

        var session = new VirtualLabSession
        {
            SchoolId        = schoolId,
            TeacherId       = teacherId,
            SessionName     = req.SessionName.Trim(),
            SessionCode     = code,
            SubjectTag      = req.SubjectTag.ToLower(),
            SceneType       = req.SceneType.ToLower(),
            IsActive        = true,
            MaxParticipants = Math.Clamp(req.MaxParticipants, 1, 100)
        };

        await _uow.VirtualLabSessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        // Reload with navigation
        var saved = await _uow.VirtualLabSessions.GetByIdAsync(
            session.Id,
            include: q => q.Include(s => s.Teacher),
            ct: ct);

        _logger.LogInformation("VirtualLab session {Code} created by teacher {TeacherId}", code, teacherId);
        return Result<LabSessionDto>.Success(MapToDto(saved!));
    }

    // ─── GetByCodeAsync ───────────────────────────────────────────────────────

    public async Task<Result<LabSessionDto>> GetByCodeAsync(
        int schoolId, string code, CancellationToken ct = default)
    {
        var sessions = await _uow.VirtualLabSessions.FindAsync(
            s => s.SchoolId == schoolId
              && s.SessionCode == code.ToUpper()
              && s.IsActive,
            include: q => q.Include(s => s.Teacher),
            ct: ct);

        var session = sessions.FirstOrDefault();
        if (session is null)
            return Result<LabSessionDto>.Failure("Mã phòng không hợp lệ hoặc phòng đã kết thúc.");

        return Result<LabSessionDto>.Success(MapToDto(session));
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    public async Task<Result<LabSessionDto>> GetByIdAsync(
        int schoolId, int sessionId, CancellationToken ct = default)
    {
        var session = await _uow.VirtualLabSessions.GetByIdAsync(
            sessionId,
            include: q => q.Include(s => s.Teacher),
            ct: ct);

        if (session is null || session.SchoolId != schoolId)
            return Result<LabSessionDto>.Failure("Không tìm thấy phòng lab.");

        return Result<LabSessionDto>.Success(MapToDto(session));
    }

    // ─── CloseSessionAsync ────────────────────────────────────────────────────

    public async Task<Result> CloseSessionAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var session = await _uow.VirtualLabSessions.GetByIdAsync(sessionId, ct);

        if (session is null || session.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy phòng lab.");

        if (session.TeacherId != teacherId)
            return Result.Failure("Chỉ giáo viên mở phòng mới có thể đóng phòng.");

        session.IsActive = false;
        _uow.VirtualLabSessions.Update(session);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("VirtualLab session {Id} closed by teacher {TeacherId}", sessionId, teacherId);
        return Result.Success();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I, O, 0, 1
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private static LabSessionDto MapToDto(VirtualLabSession s) => new(
        Id:              s.Id,
        SessionName:     s.SessionName,
        SessionCode:     s.SessionCode,
        SubjectTag:      s.SubjectTag,
        SceneType:       s.SceneType,
        TeacherId:       s.TeacherId,
        TeacherName:     s.Teacher?.FullName ?? "—",
        IsActive:        s.IsActive,
        MaxParticipants: s.MaxParticipants,
        CreatedAt:       s.CreatedAt
    );
}
