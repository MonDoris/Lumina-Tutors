using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Lab;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record CreateLabSessionRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên buổi thực hành."), MaxLength(100, ErrorMessage = "Tên buổi thực hành tối đa 100 ký tự.")] string SessionName,
    [Required(ErrorMessage = "Vui lòng chọn môn học.")]                 string SubjectTag,   // chemistry | physics | biology
    [Required(ErrorMessage = "Vui lòng chọn loại thí nghiệm.")]                 string SceneType,    // titration | pendulum | cell
    int MaxParticipants = 40
);

public record JoinLabSessionRequest(
    [Required(ErrorMessage = "Vui lòng nhập mã phòng thực hành."),
     MinLength(6, ErrorMessage = "Mã phòng 6 chữ số."),
     MaxLength(6, ErrorMessage = "Mã phòng 6 chữ số."),
     RegularExpression(@"^\d{6}$", ErrorMessage = "Mã phòng gồm 6 chữ số.")] string SessionCode
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
