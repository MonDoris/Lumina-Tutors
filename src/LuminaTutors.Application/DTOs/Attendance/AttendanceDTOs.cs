using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Attendance;

// ─── QR Session ───────────────────────────────────────────────────────────────

public record AttendanceSessionDto(
    int      SessionId,
    int      ScheduleId,
    string   ClassName,
    string   SubjectName,
    DateOnly SessionDate,
    string   SessionStatus,
    Guid     QRToken,
    DateTime QRExpiresAt,
    bool     IsQRExpired,
    string?  TopicNote,
    int      TotalStudents,
    int      PresentCount,
    int      AbsentCount,
    int      LateCount,
    int      ExcusedCount,
    DateTime CreatedAt,
    List<AttendanceRecordDto>? Records = null
);

public record CreateSessionRequest(
    [Required] int    ScheduleId,
    [Required] DateOnly SessionDate,
    int  QRExpiryMinutes = 10,
    string? TopicNote    = null
);

// ─── QR Scan (Student side) ───────────────────────────────────────────────────

public record ScanQRRequest(
    [Required] Guid QRToken,
    [Required] int  StudentId
);

public record ScanQRResponse(
    bool   Success,
    string Message,
    string StudentName,
    string SubjectName,
    string ClassName,
    DateTime CheckedInAt
);

// ─── Attendance Record ────────────────────────────────────────────────────────

public record AttendanceRecordDto(
    int    AttendanceId,
    int    StudentId,
    string StudentCode,
    string StudentName,
    string Status,
    DateTime? CheckedInAt,
    string? CheckMethod,
    bool   NotifiedParent,
    string? Note
);

public record UpdateAttendanceRequest(
    [Required] int StudentId,
    [Required] AttendanceStatus Status,
    string? Note
);

// ─── Daily Attendance Report ──────────────────────────────────────────────────

public record DailyAttendanceReportDto(
    DateOnly  ReportDate,
    string    ClassName,
    int       TotalStudents,
    int       PresentCount,
    int       AbsentCount,
    int       LateCount,
    int       ExcusedCount,
    decimal   AttendanceRate,
    List<AttendanceRecordDto> Records
);

// ─── Student Attendance Summary (for parent / admin view) ─────────────────────

public record StudentAttendanceSummaryDto(
    int    StudentId,
    string StudentName,
    int    SemesterId,
    string SemesterName,
    int    TotalSessions,
    int    PresentCount,
    int    AbsentCount,
    int    LateCount,
    int    ExcusedCount,
    decimal AttendanceRate,
    List<AbsenceDateDto> AbsenceDates
);

public record AbsenceDateDto(
    DateOnly SessionDate,
    string   SubjectName,
    string   Status
);
