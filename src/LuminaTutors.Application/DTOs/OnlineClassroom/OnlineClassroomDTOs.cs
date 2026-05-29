using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.OnlineClassroom;

// ─── Session (Room) ───────────────────────────────────────────────────────────

public record OnlineSessionDto(
    int      Id,
    string   Title,
    string?  Description,
    string   RoomCode,
    OnlineSessionStatus Status,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    int      MaxParticipants,
    int      TeacherId,
    string   TeacherName,
    int      ParticipantCount,
    int      AttendedCount
);

public record OnlineSessionListDto(
    int      Id,
    string   Title,
    string?  Description,
    string   RoomCode,
    OnlineSessionStatus Status,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    int      MaxParticipants,
    int      ParticipantCount,
    int      TeacherId,
    string   TeacherName
);

public record CreateOnlineSessionRequest(
    string   Title,
    string?  Description,
    DateTime? ScheduledAt,
    int      MaxParticipants = 50
);

public record UpdateOnlineSessionRequest(
    string   Title,
    string?  Description,
    DateTime? ScheduledAt,
    int      MaxParticipants
);

// ─── Participant ──────────────────────────────────────────────────────────────

public record ParticipantDto(
    int      UserId,
    string   FullName,
    string?  AvatarUrl,
    string   RoleCode,
    DateTime JoinedAt,
    DateTime? LeftAt,
    bool     IsAttended,
    bool     IsOnline        // tracked via SignalR connection
);

// ─── Chat Message ─────────────────────────────────────────────────────────────

public record ChatMessageDto(
    int      Id,
    int      SenderId,
    string   SenderName,
    string?  SenderAvatarUrl,
    string   Content,
    ChatMessageType MessageType,
    DateTime SentAt
);

// ─── Slide ────────────────────────────────────────────────────────────────────

public record OnlineSlideDto(
    int    Id,
    string FileName,
    string FileUrl,
    int    TotalPages,
    DateTime UploadedAt
);

// ─── Real-time Payloads (sent via SignalR) ────────────────────────────────────

public record WhiteboardStrokePayload(
    string   Tool,        // "pen" | "eraser" | "line" | "rect" | "circle" | "text"
    string   Color,
    double   LineWidth,
    double[] Points,      // flat array [x0,y0, x1,y1, ...]
    string?  Text         // for text tool
);

public record WebRtcPayload(
    int    TargetUserId,
    string Type,          // "offer" | "answer" | "ice-candidate"
    string Data           // JSON-serialized SDP or ICE candidate
);

public record AttendanceMarkedPayload(
    int    StudentUserId,
    string StudentName,
    DateTime MarkedAt
);

// ─── Join response ────────────────────────────────────────────────────────────

public record JoinRoomResult(
    OnlineSessionDto Session,
    IReadOnlyList<ParticipantDto> Participants,
    IReadOnlyList<ChatMessageDto> RecentMessages,  // last 50
    IReadOnlyList<OnlineSlideDto> Slides,
    bool IsHost
);
