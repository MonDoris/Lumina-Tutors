using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Communication;

// ─── Notification ─────────────────────────────────────────────────────────────

public class Notification : AuditableEntity
{
    public int SchoolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public int? SentByUserId { get; set; }
    public NotificationAudience TargetAudience { get; set; } = NotificationAudience.Specific;
    public int? TargetClassId { get; set; }
    public int? TargetGradeLevelId { get; set; }
    public bool IsScheduled { get; set; } = false;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    public School School { get; set; } = null!;
    public User? SentBy { get; set; }
    public Class? TargetClass { get; set; }
    public GradeLevel? TargetGradeLevel { get; set; }
    public ICollection<NotificationRecipient> Recipients { get; set; } = [];
}

// ─── NotificationRecipient ────────────────────────────────────────────────────

public class NotificationRecipient : BaseEntity
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
    public DateTime? DeliveredAt { get; set; }

    public Notification Notification { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ─── Conversation ─────────────────────────────────────────────────────────────

public class Conversation : AuditableEntity
{
    public int SchoolId { get; set; }
    public ConversationType ConversationType { get; set; } = ConversationType.Direct;
    public string? ConversationName { get; set; }    // For group chats only
    public int CreatedByUserId { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public School School { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

// ─── ConversationParticipant ──────────────────────────────────────────────────

public class ConversationParticipant : BaseEntity
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
    public bool IsAdmin { get; set; } = false;

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ─── Message ──────────────────────────────────────────────────────────────────

public class Message : BaseEntity
{
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? MessageText { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }   // IMAGE | FILE | AUDIO
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

// ─── NewsBoard ────────────────────────────────────────────────────────────────

public class NewsBoard : AuditableEntity
{
    public int SchoolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public NewsBoardScope Scope { get; set; } = NewsBoardScope.School;
    public int? TargetClassId { get; set; }
    public int? TargetGradeLevelId { get; set; }
    public bool IsPinned { get; set; } = false;
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public int PublishedByUserId { get; set; }

    public School School { get; set; } = null!;
    public Class? TargetClass { get; set; }
    public GradeLevel? TargetGradeLevel { get; set; }
    public User PublishedBy { get; set; } = null!;
}
