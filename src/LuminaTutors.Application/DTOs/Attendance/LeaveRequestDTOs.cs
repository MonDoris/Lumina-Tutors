using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Attendance;

public record LeaveRequestDto(
    int      Id,
    string   StudentName,
    string   StudentCode,
    string   ClassName,
    string   ParentName,
    DateOnly FromDate,
    DateOnly ToDate,
    int      DayCount,
    string   Reason,
    string   Status,
    string?  ReviewNote,
    string?  ReviewedBy,
    DateTime? ReviewedAt,
    DateTime  CreatedAt
);

public record CreateLeaveRequestRequest(
    [Required(ErrorMessage = "Vui lòng chọn học sinh.")]
    int StudentId,

    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")]
    DateOnly FromDate,

    [Required(ErrorMessage = "Vui lòng nhập ngày kết thúc.")]
    DateOnly ToDate,

    [Required(ErrorMessage = "Vui lòng nhập lý do xin nghỉ.")]
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự.")]
    string Reason
);

public record ReviewLeaveRequestRequest(
    [Required] int    RequestId,
    [Required] bool   Approved,
    [MaxLength(500)] string? Note
);

public record LeaveRequestListDto(
    IReadOnlyList<LeaveRequestDto> Items,
    int TotalCount,
    int PendingCount
);
