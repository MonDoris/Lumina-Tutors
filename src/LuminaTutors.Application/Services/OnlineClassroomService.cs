using LuminaTutors.Application.DTOs.OnlineClassroom;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class OnlineClassroomService : IOnlineClassroomService
{
    private readonly IUnitOfWork                       _uow;
    private readonly ILogger<OnlineClassroomService>   _logger;

    public OnlineClassroomService(IUnitOfWork uow, ILogger<OnlineClassroomService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<OnlineSessionListDto>>> GetSessionsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var sessions = await _uow.OnlineSessions.FindAsync(
            s => s.SchoolId == schoolId,
            q => q.Include(s => s.Teacher)
                  .Include(s => s.Participants)
                  .OrderByDescending(s => s.CreatedAt),
            ct);

        var dtos = sessions.Select(s => new OnlineSessionListDto(
            s.Id, s.Title, s.Description, s.RoomCode, s.Status,
            s.ScheduledAt, s.StartedAt, s.EndedAt,
            s.MaxParticipants,
            s.Participants.Count,
            s.TeacherId,
            s.Teacher.FullName
        )).ToList();

        return Result<IReadOnlyList<OnlineSessionListDto>>.Success(dtos);
    }

    public async Task<Result<OnlineSessionDto>> GetByIdAsync(
        int schoolId, int sessionId, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == sessionId,
            q => q.Include(x => x.Teacher)
                  .Include(x => x.Participants).ThenInclude(p => p.User),
            ct);

        if (s is null) return Result<OnlineSessionDto>.Failure("Không tìm thấy phòng học.");
        return Result<OnlineSessionDto>.Success(MapToDto(s));
    }

    public async Task<Result<OnlineSessionDto>> CreateAsync(
        int schoolId, int teacherId, CreateOnlineSessionRequest req, CancellationToken ct = default)
    {
        var code   = GenerateRoomCode();
        var entity = new OnlineSession
        {
            SchoolId        = schoolId,
            TeacherId       = teacherId,
            Title           = req.Title.Trim(),
            Description     = req.Description?.Trim(),
            RoomCode        = code,
            Status          = OnlineSessionStatus.Scheduled,
            ScheduledAt     = req.ScheduledAt,
            MaxParticipants = req.MaxParticipants
        };
        await _uow.OnlineSessions.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(schoolId, entity.Id, ct);
    }

    public async Task<Result<OnlineSessionDto>> UpdateAsync(
        int schoolId, int sessionId, UpdateOnlineSessionRequest req, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == sessionId, ct: ct);
        if (s is null) return Result<OnlineSessionDto>.Failure("Không tìm thấy phòng học.");
        if (s.Status == OnlineSessionStatus.Ended)
            return Result<OnlineSessionDto>.Failure("Không thể chỉnh sửa phòng đã kết thúc.");

        s.Title           = req.Title.Trim();
        s.Description     = req.Description?.Trim();
        s.ScheduledAt     = req.ScheduledAt;
        s.MaxParticipants = req.MaxParticipants;
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(schoolId, sessionId, ct);
    }

    public async Task<Result> DeleteAsync(int schoolId, int sessionId, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == sessionId, ct: ct);
        if (s is null) return Result.Failure("Không tìm thấy phòng học.");
        if (s.Status == OnlineSessionStatus.Live)
            return Result.Failure("Không thể xóa phòng đang diễn ra.");

        _uow.OnlineSessions.Remove(s);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task<Result<OnlineSessionDto>> StartSessionAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == sessionId, ct: ct);
        if (s is null) return Result<OnlineSessionDto>.Failure("Không tìm thấy phòng học.");
        if (s.TeacherId != teacherId) return Result<OnlineSessionDto>.Failure("Chỉ giáo viên tạo phòng mới được bắt đầu.");
        if (s.Status == OnlineSessionStatus.Live) return Result<OnlineSessionDto>.Failure("Phòng đang hoạt động.");
        if (s.Status == OnlineSessionStatus.Ended) return Result<OnlineSessionDto>.Failure("Phòng đã kết thúc.");

        s.Status    = OnlineSessionStatus.Live;
        s.StartedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, sessionId, ct);
    }

    public async Task<Result<OnlineSessionDto>> EndSessionAsync(
        int schoolId, int sessionId, int teacherId, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == sessionId, ct: ct);
        if (s is null) return Result<OnlineSessionDto>.Failure("Không tìm thấy phòng học.");
        if (s.TeacherId != teacherId) return Result<OnlineSessionDto>.Failure("Chỉ giáo viên tạo phòng mới được kết thúc.");

        s.Status  = OnlineSessionStatus.Ended;
        s.EndedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, sessionId, ct);
    }

    public async Task<Result<JoinRoomResult>> JoinByCodeAsync(
        int schoolId, int userId, string roomCode, CancellationToken ct = default)
    {
        var s = await _uow.OnlineSessions.FindOneAsync(
            x => x.SchoolId == schoolId && x.RoomCode == roomCode.ToUpper().Trim(),
            q => q.Include(x => x.Teacher)
                  .Include(x => x.Participants).ThenInclude(p => p.User),
            ct);

        if (s is null)  return Result<JoinRoomResult>.Failure("Mã phòng không hợp lệ.");
        if (s.Status == OnlineSessionStatus.Ended)
            return Result<JoinRoomResult>.Failure("Phòng học đã kết thúc.");
        if (s.Participants.Count(p => p.LeftAt == null) >= s.MaxParticipants && s.TeacherId != userId)
            return Result<JoinRoomResult>.Failure("Phòng học đã đầy.");

        // Record join
        await RecordJoinAsync(s.Id, userId, ct);

        var participants = await GetParticipantsAsync(s.Id, ct);
        var messages     = await GetChatHistoryAsync(s.Id, 50, ct);
        var slides       = await GetSlidesAsync(s.Id, ct);

        var result = new JoinRoomResult(
            MapToDto(s),
            participants.Data!,
            messages.Data!,
            slides.Data!,
            IsHost: s.TeacherId == userId
        );
        return Result<JoinRoomResult>.Success(result);
    }

    // ── Participants ──────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ParticipantDto>>> GetParticipantsAsync(
        int sessionId, CancellationToken ct = default)
    {
        var list = await _uow.SessionParticipants.FindAsync(
            p => p.SessionId == sessionId,
            q => q.Include(p => p.User).OrderBy(p => p.JoinedAt),
            ct);

        var user = await _uow.Users.FindAsync(u => true, ct: ct); // load all - small set
        var dtos = list.Select(p => new ParticipantDto(
            p.UserId,
            p.User.FullName,
            p.User.AvatarUrl,
            p.User.Role?.RoleCode ?? "STUDENT",
            p.JoinedAt,
            p.LeftAt,
            p.IsAttended,
            IsOnline: p.LeftAt == null
        )).ToList();

        return Result<IReadOnlyList<ParticipantDto>>.Success(dtos);
    }

    public async Task<Result> RecordJoinAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        // Upsert: if already has open record, keep it; otherwise create new
        var existing = await _uow.SessionParticipants.FindOneAsync(
            p => p.SessionId == sessionId && p.UserId == userId && p.LeftAt == null, ct: ct);

        if (existing is null)
        {
            var p = new SessionParticipant { SessionId = sessionId, UserId = userId };
            await _uow.SessionParticipants.AddAsync(p, ct);
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result> RecordLeaveAsync(int sessionId, int userId, CancellationToken ct = default)
    {
        var p = await _uow.SessionParticipants.FindOneAsync(
            x => x.SessionId == sessionId && x.UserId == userId && x.LeftAt == null, ct: ct);
        if (p is null) return Result.Success(); // already left or never joined

        p.LeftAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkAttendanceAsync(
        int sessionId, int studentUserId, int markedByUserId, CancellationToken ct = default)
    {
        var p = await _uow.SessionParticipants.FindOneAsync(
            x => x.SessionId == sessionId && x.UserId == studentUserId, ct: ct);
        if (p is null) return Result.Failure("Học sinh không có trong phòng.");

        p.IsAttended = true;
        p.AttendedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Chat ──────────────────────────────────────────────────────────────────

    public async Task<Result<ChatMessageDto>> SaveChatMessageAsync(
        int sessionId, int senderId, string content, CancellationToken ct = default)
    {
        var msg = new OnlineRoomChat
        {
            SessionId   = sessionId,
            SenderId    = senderId,
            Content     = content.Trim(),
            MessageType = ChatMessageType.Text,
            SentAt      = DateTime.UtcNow
        };
        await _uow.OnlineRoomChats.AddAsync(msg, ct);
        await _uow.SaveChangesAsync(ct);

        var sender = await _uow.Users.GetByIdAsync(senderId, ct);
        return Result<ChatMessageDto>.Success(new ChatMessageDto(
            msg.Id, senderId,
            sender?.FullName ?? "Unknown",
            sender?.AvatarUrl,
            content,
            ChatMessageType.Text,
            msg.SentAt
        ));
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> GetChatHistoryAsync(
        int sessionId, int pageSize = 50, CancellationToken ct = default)
    {
        var msgs = await _uow.OnlineRoomChats.FindAsync(
            m => m.SessionId == sessionId,
            q => q.Include(m => m.Sender)
                  .OrderByDescending(m => m.SentAt)
                  .Take(pageSize),
            ct);

        var dtos = msgs
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto(
                m.Id, m.SenderId,
                m.Sender.FullName,
                m.Sender.AvatarUrl,
                m.Content,
                m.MessageType,
                m.SentAt))
            .ToList();

        return Result<IReadOnlyList<ChatMessageDto>>.Success(dtos);
    }

    // ── Slides ────────────────────────────────────────────────────────────────

    public async Task<Result<OnlineSlideDto>> UploadSlideAsync(
        int sessionId, string fileName, string fileUrl, int totalPages, CancellationToken ct = default)
    {
        var slide = new OnlineSlide
        {
            SessionId  = sessionId,
            FileName   = fileName,
            FileUrl    = fileUrl,
            TotalPages = totalPages
        };
        await _uow.OnlineSlides.AddAsync(slide, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<OnlineSlideDto>.Success(new OnlineSlideDto(
            slide.Id, slide.FileName, slide.FileUrl, slide.TotalPages, slide.UploadedAt));
    }

    public async Task<Result<IReadOnlyList<OnlineSlideDto>>> GetSlidesAsync(
        int sessionId, CancellationToken ct = default)
    {
        var slides = await _uow.OnlineSlides.FindAsync(
            s => s.SessionId == sessionId,
            q => q.OrderBy(s => s.UploadedAt),
            ct);

        var dtos = slides.Select(s => new OnlineSlideDto(
            s.Id, s.FileName, s.FileUrl, s.TotalPages, s.UploadedAt)).ToList();

        return Result<IReadOnlyList<OnlineSlideDto>>.Success(dtos);
    }

    public async Task<Result> DeleteSlideAsync(int sessionId, int slideId, CancellationToken ct = default)
    {
        var slide = await _uow.OnlineSlides.FindOneAsync(
            s => s.SessionId == sessionId && s.Id == slideId, ct: ct);
        if (slide is null) return Result.Failure("Không tìm thấy slide.");

        _uow.OnlineSlides.Remove(slide);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OnlineSessionDto MapToDto(OnlineSession s) => new(
        s.Id, s.Title, s.Description, s.RoomCode, s.Status,
        s.ScheduledAt, s.StartedAt, s.EndedAt,
        s.MaxParticipants,
        s.TeacherId,
        s.Teacher?.FullName ?? "",
        s.Participants?.Count ?? 0,
        s.Participants?.Count(p => p.IsAttended) ?? 0
    );

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng  = new Random();
        var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        return $"{part1}-{part2}";
    }
}
