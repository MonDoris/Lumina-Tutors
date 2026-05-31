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
    [Required(ErrorMessage = "Vui lòng nhập tên lớp."), MaxLength(20, ErrorMessage = "Tên lớp tối đa 20 ký tự.")] string ClassName,
    [Required(ErrorMessage = "Vui lòng chọn khối lớp.")] int GradeLevelId,
    [Required(ErrorMessage = "Vui lòng chọn năm học.")] int AcademicYearId,
    int?  HomeRoomTeacherId,
    byte  MaxStudents = 40,
    string? RoomNumber = null
);

public record UpdateClassRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên lớp."), MaxLength(20, ErrorMessage = "Tên lớp tối đa 20 ký tự.")] string ClassName,
    int?   HomeRoomTeacherId,
    byte   MaxStudents,
    string? RoomNumber
);

public record AssignSubjectRequest(
    [Required(ErrorMessage = "Vui lòng chọn môn học.")] int SubjectId,
    [Required(ErrorMessage = "Vui lòng chọn giáo viên.")] int TeacherId,
    [Required(ErrorMessage = "Vui lòng chọn học kỳ.")] int SemesterId,
    byte PeriodsPerWeek = 2
);

public record CreateScheduleRequest(
    [Required(ErrorMessage = "Vui lòng chọn phân công môn học.")] int  SubjectAssignmentId,
    [Required(ErrorMessage = "Vui lòng chọn thứ trong tuần.")] byte DayOfWeek,
    [Required(ErrorMessage = "Vui lòng nhập tiết bắt đầu.")] byte PeriodStart,
    [Required(ErrorMessage = "Vui lòng nhập tiết kết thúc.")] byte PeriodEnd,
    [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu.")] TimeOnly StartTime,
    [Required(ErrorMessage = "Vui lòng nhập giờ kết thúc.")] TimeOnly EndTime,
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
    [Required(ErrorMessage = "Vui lòng nhập tên năm học."), MaxLength(20, ErrorMessage = "Tên năm học tối đa 20 ký tự.")] string YearName,
    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")] DateOnly StartDate,
    [Required(ErrorMessage = "Vui lòng nhập ngày kết thúc.")] DateOnly EndDate,
    bool IsActive = false
);

public record CreateGradeLevelRequest(
    [Required(ErrorMessage = "Vui lòng nhập số khối."), Range(1, 12, ErrorMessage = "Số khối phải từ 1 đến 12.")] byte GradeNumber,
    [Required(ErrorMessage = "Vui lòng nhập tên khối."), MaxLength(20, ErrorMessage = "Tên khối tối đa 20 ký tự.")] string GradeName,
    [Required(ErrorMessage = "Vui lòng chọn cấp học.")] string EducationLevel
);

// ─── Select List DTOs ─────────────────────────────────────────────────────────

public record GradeLevelSelectDto(int GradeLevelId, string GradeName, byte GradeNumber);
public record AcademicYearSelectDto(int AcademicYearId, string YearName, bool IsActive);
public record SubjectSelectDto(int SubjectId, string SubjectName, string SubjectCode);
public record SemesterSelectDto(int SemesterId, string SemesterName, bool IsActive);

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
