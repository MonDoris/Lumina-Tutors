using LuminaTutors.Application.DTOs.Support;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Application.Services;

/// <summary>
/// Kênh hỗ trợ Nhà trường ↔ Quản trị hệ thống. Tận dụng bảng Conversation/Message
/// sẵn có; luồng hỗ trợ được đánh dấu bằng ConversationName = "__SUPPORT__".
/// SYSADMIN được thêm làm participant để theo dõi "đã đọc".
/// </summary>
public sealed class SupportChatService : ISupportChatService
{
    private const string SupportMarker = "__SUPPORT__";
    private readonly IUnitOfWork _uow;

    public SupportChatService(IUnitOfWork uow) => _uow = uow;

    private async Task<User?> GetSysAdminAsync(CancellationToken ct)
        => (await _uow.Users.FindAsync(u => u.Role.RoleCode == "SYSADMIN", ct: ct)).FirstOrDefault();

    private async Task<Conversation?> FindThreadAsync(int schoolId, CancellationToken ct)
        => await _uow.Conversations.FindOneAsync(
            c => c.SchoolId == schoolId && c.ConversationName == SupportMarker,
            include: q => q.Include(c => c.Participants), ct: ct);

    public async Task<Result<SupportThreadDto>> GetSchoolThreadAsync(
        int schoolId, bool markReadForSysAdmin, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result<SupportThreadDto>.Failure("Trường không tồn tại.", "NOT_FOUND");

        var conv = await FindThreadAsync(schoolId, ct);
        if (conv is null)
            return Result<SupportThreadDto>.Success(
                new SupportThreadDto(null, schoolId, school.SchoolName, new List<SupportMessageDto>()));

        var sysId = (await GetSysAdminAsync(ct))?.Id ?? 0;

        var msgs = await _uow.Messages.FindAsync(
            m => m.ConversationId == conv.Id && !m.IsDeleted,
            include: q => q.Include(m => m.Sender), ct: ct);

        var list = msgs.OrderBy(m => m.SentAt)
            .Select(m => new SupportMessageDto(
                m.Id, m.SenderId, m.Sender != null ? m.Sender.FullName : "", m.SenderId == sysId,
                m.MessageText ?? "", m.SentAt))
            .ToList();

        if (markReadForSysAdmin && sysId > 0)
        {
            var part = conv.Participants.FirstOrDefault(p => p.UserId == sysId);
            if (part is not null) { part.LastReadAt = DateTime.UtcNow; await _uow.SaveChangesAsync(ct); }
        }

        return Result<SupportThreadDto>.Success(
            new SupportThreadDto(conv.Id, schoolId, school.SchoolName, list));
    }

    public async Task<Result> SendAsync(int schoolId, int senderUserId, string text, CancellationToken ct = default)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return Result.Failure("Nội dung tin nhắn trống.", "EMPTY");

        var sys = await GetSysAdminAsync(ct);
        if (sys is null) return Result.Failure("Chưa cấu hình tài khoản Quản trị hệ thống.", "NO_SYSADMIN");

        var conv = await FindThreadAsync(schoolId, ct);
        if (conv is null)
        {
            conv = new Conversation
            {
                SchoolId         = schoolId,
                ConversationType = ConversationType.Direct,
                ConversationName = SupportMarker,
                CreatedByUserId  = senderUserId,
                LastMessageAt    = DateTime.UtcNow
            };
            await _uow.Conversations.AddAsync(conv, ct);
            await _uow.SaveChangesAsync(ct);   // để có conv.Id

            await _uow.ConversationParticipants.AddAsync(new ConversationParticipant
            {
                ConversationId = conv.Id,
                UserId         = sys.Id,
                IsAdmin        = true
            }, ct);
        }

        await _uow.Messages.AddAsync(new Message
        {
            ConversationId = conv.Id,
            SenderId       = senderUserId,
            MessageText    = text,
            SentAt         = DateTime.UtcNow
        }, ct);
        conv.LastMessageAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SupportThreadListItemDto>>> GetAllThreadsAsync(CancellationToken ct = default)
    {
        var sysId = (await GetSysAdminAsync(ct))?.Id ?? 0;

        var convs = await _uow.Conversations.FindAsync(
            c => c.ConversationName == SupportMarker,
            include: q => q.Include(c => c.School)
                           .Include(c => c.Messages)
                           .Include(c => c.Participants), ct: ct);

        var items = convs.Select(c =>
        {
            var msgs     = c.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.SentAt).ToList();
            var last     = msgs.LastOrDefault();
            var lastRead = c.Participants.FirstOrDefault(p => p.UserId == sysId)?.LastReadAt;
            var unread   = msgs.Count(m => m.SenderId != sysId && (lastRead == null || m.SentAt > lastRead));
            return new SupportThreadListItemDto(
                c.SchoolId,
                c.School != null ? c.School.SchoolName : $"Trường #{c.SchoolId}",
                c.Id, last?.MessageText, last?.SentAt, unread);
        })
        .OrderByDescending(x => x.LastAt)
        .ToList();

        return Result<IReadOnlyList<SupportThreadListItemDto>>.Success(items);
    }

    public async Task<int> GetUnreadForSysAdminAsync(CancellationToken ct = default)
    {
        var r = await GetAllThreadsAsync(ct);
        return r.IsSuccess ? r.Data!.Sum(x => x.Unread) : 0;
    }
}
