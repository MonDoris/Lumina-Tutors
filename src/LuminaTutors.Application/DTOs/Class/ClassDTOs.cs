using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Class;

// ─── Class DTOs ───────────────────────────────────────────────────────────────

public record ClassSummaryDto(
    int    ClassId,
    string ClassName,
    string GradeName,
    string AcademicYearName,
    string? HomeRoomTeacherName,
    int    EnrolledCount,
    int    MaxStudents,
    bool   IsActive
);

public record ClassDetailDto(
    int    ClassId,
    string ClassName,
    int    GradeLevelId,
    string GradeName,
    string EducationLevel,
    int    AcademicYearId,
    string AcademicYearName,
    int?   HomeRoomTeacherId,
    string? HomeRoomTeacherName,
    string? RoomNumber,
    int    MaxStudents,
    bool   IsActive,
    List<SubjectAssignmentDto> SubjectAssignments,
    List<ScheduleSlotDto>      Schedule,
    List<ClassStudentDto>      Students
);

public record ClassStudentDto(
    int    UserId,
    string FullName,
    string StudentCode,
    string? PhoneNumber,
    string? AvatarUrl,
    DateOnly EnrolledDate
);

public record SubjectAssignmentDto(
    int    AssignmentId,
    string SubjectName,
    string SubjectCode,
    string TeacherName,
    byte   PeriodsPerWeek
);

public record ScheduleSlotDto(
    int    ScheduleId,
    string SubjectName,
    string TeacherName,
    byte   DayOfWeek,
    string DayName,
    byte   PeriodStart,
    byte   PeriodEnd,
    string StartTime,
    string EndTime,
    string? RoomOverride
);

public record CreateClassRequest(
    [Required, MaxLength(20)] string ClassName,
    [Required] int GradeLevelId,
    [Required] int AcademicYearId,
    int?  HomeRoomTeacherId,
    byte  MaxStudents = 40,
    string? RoomNumber = null
);

public record UpdateClassRequest(
    [Required, MaxLength(20)] string ClassName,
    int?   HomeRoomTeacherId,
    byte   MaxStudents,
    string? RoomNumber
);

public record AssignSubjectRequest(
    [Required] int SubjectId,
    [Required] int TeacherId,
    [Required] int SemesterId,
    byte PeriodsPerWeek = 2
);

public record CreateScheduleRequest(
    [Required] int  SubjectAssignmentId,
    [Required] byte DayOfWeek,
    [Required] byte PeriodStart,
    [Required] byte PeriodEnd,
    [Required] TimeOnly StartTime,
    [Required] TimeOnly EndTime,
    string? RoomOverride
);

// ─── Academic configuration (Admin) ───────────────────────────────────────────

public record AcademicYearConfigDto(
    int AcademicYearId,
    string YearName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive
);

public record GradeLevelConfigDto(
    int GradeLevelId,
    byte GradeNumber,
    string GradeName,
    string EducationLevel
);

public record CreateAcademicYearRequest(
    [Required, MaxLength(20)] string YearName,
    [Required] DateOnly StartDate,
    [Required] DateOnly EndDate,
    bool IsActive = false
);

public record CreateGradeLevelRequest(
    [Required, Range(1, 12)] byte GradeNumber,
    [Required, MaxLength(20)] string GradeName,
    [Required] string EducationLevel
);

// ─── Select List DTOs ─────────────────────────────────────────────────────────

public record GradeLevelSelectDto(int GradeLevelId, string GradeName, byte GradeNumber);
public record AcademicYearSelectDto(int AcademicYearId, string YearName, bool IsActive);
public record SubjectSelectDto(int SubjectId, string SubjectName, string SubjectCode);

// ─── Teacher DTOs ──────────────────────────────────────────────────────────────

public record TeacherSummaryDto(
    int    UserId,
    string TeacherCode,
    string FullName,
    string? PhoneNumber,
    string? SpecializationSubject,
    string? Qualification,
    string? ContractType,
    bool   IsActive
);
