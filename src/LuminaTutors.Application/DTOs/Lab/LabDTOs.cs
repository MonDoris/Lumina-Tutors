using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Lab;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record CreateLabSessionRequest(
    [Required, MaxLength(100)] string SessionName,
    [Required]                 string SubjectTag,   // chemistry | physics | biology
    [Required]                 string SceneType,    // titration | pendulum | cell
    int MaxParticipants = 40
);

public record JoinLabSessionRequest(
    [Required, MinLength(6), MaxLength(6)] string SessionCode
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record LabSessionDto(
    int      Id,
    string   SessionName,
    string   SessionCode,
    string   SubjectTag,
    string   SceneType,
    int      TeacherId,
    string   TeacherName,
    bool     IsActive,
    int      MaxParticipants,
    DateTime CreatedAt
);
