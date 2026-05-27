using AutoMapper;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork                 _uow;
    private readonly IMapper                     _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork uow, IMapper mapper, ILogger<NotificationService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── Send ─────────────────────────────────────────────────────────────────

    public async Task<Result> SendAsync(
        int schoolId, int? sentByUserId, SendNotificationRequest request, CancellationToken ct = default)
    {
        // Determine target user IDs based on audience
        IList<int> targetUserIds = new List<int>();

        switch (request.TargetAudience)
        {
            case NotificationAudience.SchoolAll:
                var allUsers = await _uow.Users.FindAsync(
                    u => u.SchoolId == schoolId && u.IsActive, ct: ct);
                targetUserIds = allUsers.Select(u => u.Id).ToList();
                break;

            case NotificationAudience.Specific:
                if (request.TargetUserIds?.Any() == true)
                    targetUserIds = request.TargetUserIds;
                break;

            case NotificationAudience.Class:
                if (request.TargetClassId.HasValue)
                {
                    var classEnrollments = await _uow.ClassEnrollments.FindAsync(
                        e => e.ClassId == request.TargetClassId.Value &&
                             e.Status == EnrollmentStatus.Active,
                        ct: ct);
                    targetUserIds = classEnrollments.Select(e => e.StudentId).ToList();
                }
                break;

            case NotificationAudience.Grade:
                if (request.TargetGradeLevelId.HasValue)
                {
                    var gradeEnrollments = await _uow.ClassEnrollments.FindAsync(
                        e => e.Class.GradeLevelId == request.TargetGradeLevelId.Value &&
                             e.Status == EnrollmentStatus.Active,
                        include: q => q.Include(e => e.Class),
                        ct: ct);
                    targetUserIds = gradeEnrollments.Select(e => e.StudentId).Distinct().ToList();
                }
                break;
        }

        if (!targetUserIds.Any())
            return Result.Failure("NO_TARGETS", "Không có người nhận nào được xác định.");

        // Create a single Notification broadcast record
        var notification = new Notification
        {
            SchoolId           = schoolId,
            SentByUserId       = sentByUserId,
            Title              = request.Title.Trim(),
            Body               = request.Body.Trim(),
            NotificationType   = request.NotificationType,
            Channel            = request.Channel,
            TargetAudience     = request.TargetAudience,
            TargetClassId      = request.TargetClassId,
            TargetGradeLevelId = request.TargetGradeLevelId,
            SentAt             = DateTime.UtcNow
        };

        await _uow.Notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        // Create per-user recipient records
        var recipients = targetUserIds.Select(userId => new NotificationRecipient
        {
            NotificationId  = notification.Id,
            UserId          = userId,
            IsRead          = false,
            DeliveryStatus  = DeliveryStatus.Delivered,
            DeliveredAt     = DateTime.UtcNow
        }).ToList();

        await _uow.NotificationRecipients.AddRangeAsync(recipients, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notification sent to {Count} users by user {SentBy} in school {SchoolId}",
            recipients.Count, sentByUserId, schoolId);

        return Result.Success();
    }

    // ─── GetForUser ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var paged = await _uow.NotificationRecipients.GetPagedAsync(
            pageNumber: page,
            pageSize:   pageSize,
            filter:  r => r.UserId == userId,
            orderBy: q => q.OrderByDescending(r => r.Notification.SentAt),
            include: q => q.Include(r => r.Notification),
            ct: ct);

        var dtos = _mapper.Map<List<NotificationDto>>(paged.Items);
        return Result<IReadOnlyList<NotificationDto>>.Success(dtos);
    }

    // ─── GetUnreadCount ───────────────────────────────────────────────────────

    public async Task<Result<int>> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        var unread = await _uow.NotificationRecipients.FindAsync(
            r => r.UserId == userId && !r.IsRead, ct: ct);

        return Result<int>.Success(unread.Count);
    }

    // ─── MarkRead ─────────────────────────────────────────────────────────────

    public async Task<Result> MarkReadAsync(
        int userId, int notificationId, CancellationToken ct = default)
    {
        var recipients = await _uow.NotificationRecipients.FindAsync(
            r => r.NotificationId == notificationId && r.UserId == userId, ct: ct);

        var recipient = recipients.FirstOrDefault();
        if (recipient is null)
            return Result.Failure("NOT_FOUND", "Thông báo không tồn tại.");

        recipient.IsRead  = true;
        recipient.ReadAt  = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ─── MarkAllRead ──────────────────────────────────────────────────────────

    public async Task<Result> MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        var unread = await _uow.NotificationRecipients.FindAsync(
            r => r.UserId == userId && !r.IsRead, ct: ct);

        var now = DateTime.UtcNow;
        foreach (var r in unread)
        {
            r.IsRead = true;
            r.ReadAt = now;
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
