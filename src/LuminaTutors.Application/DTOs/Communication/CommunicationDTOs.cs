using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Communication;

// ─── Notification ─────────────────────────────────────────────────────────────

public record NotificationDto(
    int      NotificationId,
    string   Title,
    string   Body,
    string   NotificationType,
    string   Channel,
    string?  SentByName,
    DateTime CreatedAt,
    bool     IsRead
);

public record SendNotificationRequest(
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề thông báo."), MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")] string Title,
    [Required(ErrorMessage = "Vui lòng nhập nội dung thông báo.")] string Body,
    [Required(ErrorMessage = "Vui lòng chọn loại thông báo.")] NotificationType NotificationType,
    NotificationChannel  Channel        = NotificationChannel.InApp,
    NotificationAudience TargetAudience = NotificationAudience.Specific,
    List<int>? TargetUserIds   = null,
    int?       TargetClassId   = null,
    int?       TargetGradeLevelId = null
);

// ─── Messages / Chat ──────────────────────────────────────────────────────────

public record ConversationDto(
    int      ConversationId,
    string   ConversationType,
    string?  ConversationName,
    string   OtherPartyName,
    string?  OtherPartyAvatar,
    string?  LastMessage,
    DateTime? LastMessageAt,
    int      UnreadCount
);

public record MessageDto(
    long     MessageId,
    int      SenderId,
    string   SenderName,
    string?  SenderAvatar,
    string?  MessageText,
    string?  AttachmentUrl,
    string?  AttachmentType,
    bool     IsDeleted,
    DateTime SentAt,
    bool     IsMine
);

public record SendMessageRequest(
    [Required(ErrorMessage = "Vui lòng chọn cuộc trò chuyện.")] int     ConversationId,
    string?            MessageText,
    string?            AttachmentUrl,
    string?            AttachmentType
);

public record StartConversationRequest(
    [Required(ErrorMessage = "Vui lòng chọn người nhận.")] int RecipientUserId,
    string?        InitialMessage = null
);

// ─── News Board ───────────────────────────────────────────────────────────────

public record NewsBoardDto(
    int      NewsId,
    string   Title,
    string   ContentHtml,
    string?  CoverImageUrl,
    string   Scope,
    string?  TargetClassName,
    bool     IsPinned,
    bool     IsPublished,
    string   PublishedByName,
    DateTime? PublishedAt,
    DateTime CreatedAt
);

public record CreateNewsRequest(
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết."), MaxLength(300, ErrorMessage = "Tiêu đề tối đa 300 ký tự.")] string Title,
    [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết.")] string ContentHtml,
    string?        CoverImageUrl   = null,
    NewsBoardScope Scope           = NewsBoardScope.School,
    int?           TargetClassId   = null,
    int?           TargetGradeLevelId = null,
    bool           IsPinned        = false,
    bool           PublishNow      = false
);
