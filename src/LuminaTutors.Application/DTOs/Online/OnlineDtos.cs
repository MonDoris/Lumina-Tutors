namespace LuminaTutors.Application.DTOs.Online;

// ─── Session list / detail ────────────────────────────────────────────────────

public record OnlineSessionDto(
    int      SessionId,
    int      SchoolId,
    int      TeacherId,
    string   TeacherName,
    string   Title,
    string?  Description,
    string   RoomCode,
    string   Status,           // "Scheduled" | "Live" | "Ended" | "Cancelled"
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    int      MaxParticipants,
    int      ParticipantCount, // distinct users who joined
    DateTime CreatedAt
);

public record OnlineSessionParticipantDto(
    int      UserId,
    string   FullName,
    string   Role,
    DateTime JoinedAt,
    DateTime? LeftAt
);

// ─── Create / Update ──────────────────────────────────────────────────────────

public record CreateOnlineSessionRequest(
    string   Title,
    string?  Description,
    DateTime? ScheduledAt,
    int      MaxParticipants
);

// ─── Join event ───────────────────────────────────────────────────────────────

public record JoinSessionResult(
    int    SessionId,
    string RoomCode,
    string JitsiRoom,          // full room name for Jitsi: "lumina-{schoolId}-{code}"
    string Title,
    string TeacherName,
    string Status,
    int    SchoolId
);
