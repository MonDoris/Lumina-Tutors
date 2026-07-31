using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Course;

// ═════════════════════════════════════════════════════════════════════════════
//  E-Learning DTOs — Course / Module / Lesson / Enrollment / Progress
// ═════════════════════════════════════════════════════════════════════════════

// ── Teacher/Admin: quản trị khóa học ─────────────────────────────────────────

public record CourseListItemDto(
    int          Id,
    string       Title,
    string?      ThumbnailUrl,
    string?      SubjectName,
    string?      GradeLevelName,
    CourseStatus Status,
    bool         IsSequential,
    int          ModuleCount,
    int          LessonCount,
    int          EnrolledCount,
    decimal      AvgProgressPercent,
    DateTime     UpdatedAt
);

public record CourseDetailDto(
    int          Id,
    string       Title,
    string?      Description,
    string?      ThumbnailUrl,
    int?         SubjectId,
    int?         GradeLevelId,
    string?      SubjectName,
    string?      GradeLevelName,
    CourseStatus Status,
    bool         IsSequential,
    DateTime?    PublishedAt,
    string       CreatedByName,
    List<CourseModuleDto> Modules
);

public record CourseModuleDto(
    int       Id,
    string    Title,
    string?   Description,
    int       SortOrder,
    int?      UnlockAfterDays,
    DateTime? AvailableFrom,
    byte?     SemesterNo,
    byte?     StartWeek,
    List<CourseLessonDto> Lessons
);

public record CourseLessonDto(
    int              Id,
    string           Title,
    int              SortOrder,
    CourseLessonType ContentType,
    string?          ContentHtml,
    string?          VideoUrl,
    int?             VideoDurationSec,
    int?             QuizExamId,
    byte             MinWatchPercent,
    bool             IsPreviewable,
    bool             IsPublished,
    string?          Objectives,
    CognitiveLevel?  CognitiveLevel,
    byte             PeriodCount,
    int              MaterialCount
);

public record CreateCourseRequest(
    string  Title,
    string? Description,
    string? ThumbnailUrl,
    int?    SubjectId,
    int?    GradeLevelId,
    bool    IsSequential
);

public record UpdateCourseRequest(
    string  Title,
    string? Description,
    string? ThumbnailUrl,
    int?    SubjectId,
    int?    GradeLevelId,
    bool    IsSequential
);

/// <summary>Id = null → tạo mới; Id có giá trị → cập nhật.</summary>
public record SaveModuleRequest(
    int?      Id,
    int       CourseId,
    string    Title,
    string?   Description,
    int?      UnlockAfterDays,
    DateTime? AvailableFrom,
    byte?     SemesterNo = null,
    byte?     StartWeek  = null
);

/// <summary>Id = null → tạo mới; Id có giá trị → cập nhật.</summary>
public record SaveLessonRequest(
    int?             Id,
    int              ModuleId,
    string           Title,
    CourseLessonType ContentType,
    string?          ContentHtml,
    string?          VideoUrl,
    int?             VideoDurationSec,
    int?             QuizExamId,
    byte             MinWatchPercent = 90,
    bool             IsPreviewable   = false,
    bool             IsPublished     = false,
    string?          Objectives      = null,
    CognitiveLevel?  CognitiveLevel  = null,
    byte             PeriodCount     = 1
);

public record ClassAssignmentDto(
    int       Id,
    int       ClassId,
    string    ClassName,
    string    AssignedByName,
    DateTime  AssignedAt,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool      IsActive,
    int       EnrolledCount,
    decimal   AvgProgressPercent
);

// ── Teacher/Admin: thống kê hoàn thành ───────────────────────────────────────

/// <summary>Thống kê mức độ hoàn thành của học sinh trong 1 khóa (không tính Dropped).</summary>
public record CourseStatsDto(
    decimal AvgProgressPercent,
    int     TotalActiveStudents,
    int     CompletedStudents,
    int     Bucket0To25,      // số HS đạt [0–25%)
    int     Bucket25To50,     // [25–50%)
    int     Bucket50To75,     // [50–75%)
    int     Bucket75To100     // [75–100%]
);

/// <summary>Tỷ lệ hoàn thành theo TỪNG bài học — phát hiện bài học sinh hay bỏ dở.</summary>
public record LessonStatRowDto(
    int              LessonId,
    string           ModuleTitle,
    string           LessonTitle,
    CourseLessonType ContentType,
    int              CompletedCount,
    int              InProgressCount,
    decimal          CompletionRate    // % trên tổng HS đang hoạt động
);

// ── Teacher/Admin: báo cáo tiến độ ───────────────────────────────────────────

public record CourseProgressReportDto(
    int    CourseId,
    string CourseTitle,
    int    TotalPublishedLessons,
    int    EnrolledCount,
    int    CompletedCount,
    CourseStatsDto Stats,
    List<StudentProgressRowDto> Students,
    List<LessonStatRowDto>      LessonStats
);

public record StudentProgressRowDto(
    int       StudentId,
    string    FullName,
    string?   ClassName,
    decimal   ProgressPercent,
    int       CompletedLessons,
    CourseEnrollmentStatus Status,
    DateTime? StartedAt,
    DateTime? LastAccessedAt,
    DateTime? CompletedAt
);

// ── Nhà trường (Admin): giám sát khóa học theo giáo viên ─────────────────────

/// <summary>
/// Một giáo viên (người phụ trách khóa) kèm toàn bộ khóa học của họ.
/// Nhà trường chỉ XEM — không tạo/sửa nội dung khóa.
/// </summary>
public record TeacherCourseGroupDto(
    int     TeacherId,
    string  TeacherName,
    string  RoleLabel,
    int     CourseCount,
    int     ClassCount,
    int     EnrollmentCount,
    decimal AvgProgressPercent,
    List<ManagedCourseDto> Courses
);

public record ManagedCourseDto(
    int          CourseId,
    string       Title,
    string?      SubjectName,
    string?      GradeLevelName,
    CourseStatus Status,
    int          LessonCount,
    int          QuizLessonCount,
    int          EnrollmentCount,
    decimal      AvgProgressPercent,
    List<ManagedClassRowDto> Classes
);

/// <summary>Một lớp được gán khóa học — số học sinh và % hoàn thành của riêng lớp đó.</summary>
public record ManagedClassRowDto(
    int     ClassId,
    string  ClassName,
    bool    IsActive,
    int     StudentCount,
    int     CompletedCount,
    decimal AvgProgressPercent
);

/// <summary>Bài tập trên lớp (Assignment) thuộc môn của khóa — kèm tỷ lệ nộp bài.</summary>
public record ClassHomeworkRowDto(
    int            AssignmentId,
    string         Title,
    AssignmentType Type,
    string         TeacherName,
    DateTime?      DueDate,
    bool           IsPublished,
    int            SubmittedCount,
    int            GradedCount,
    int            ClassSize,
    decimal        SubmittedPercent,
    decimal?       AvgScore,
    decimal        MaxScore
);

/// <summary>Khóa học × 1 lớp: bài học trong khóa + bài tập trên lớp + % của từng học sinh.</summary>
public record ClassCourseProgressDto(
    int          CourseId,
    string       CourseTitle,
    int          ClassId,
    string       ClassName,
    string       TeacherName,
    string?      SubjectName,
    CourseStatus Status,
    int          TotalPublishedLessons,
    int          ClassSize,
    CourseStatsDto              Stats,
    List<LessonStatRowDto>      Lessons,
    List<ClassHomeworkRowDto>   Homework,
    List<StudentProgressRowDto> Students
);

// ── Student: học tập ─────────────────────────────────────────────────────────

public record MyCourseDto(
    int      CourseId,
    int      EnrollmentId,
    string   Title,
    string?  ThumbnailUrl,
    string?  SubjectName,
    decimal  ProgressPercent,
    int      CompletedLessons,
    int      TotalLessons,
    int?     LastLessonId,
    CourseEnrollmentStatus Status,
    DateTime? LastAccessedAt
);

public record StudentCourseOutlineDto(
    int     CourseId,
    int     EnrollmentId,
    string  Title,
    string? Description,
    bool    IsSequential,
    decimal ProgressPercent,
    int     CompletedLessons,
    int     TotalLessons,
    int?    ResumeLessonId,
    List<StudentModuleDto> Modules
);

public record StudentModuleDto(
    int     Id,
    string  Title,
    string? Description,
    int     SortOrder,
    bool    IsLocked,
    string? LockReason,
    List<StudentLessonRowDto> Lessons
);

public record StudentLessonRowDto(
    int              Id,
    string           Title,
    int              SortOrder,
    CourseLessonType ContentType,
    int?             VideoDurationSec,
    bool             IsLocked,
    ProgressStatus?  Status          // null = chưa bắt đầu
);

public record StudentLessonContentDto(
    int              Id,
    int              ModuleId,
    string           Title,
    CourseLessonType ContentType,
    string?          ContentHtml,
    string?          VideoUrl,
    int?             VideoDurationSec,
    int?             QuizExamId,
    byte             MinWatchPercent,
    string?          Objectives,
    int              LastPositionSec,
    int              WatchedSec,
    bool             IsCompleted,
    int?             PrevLessonId,
    int?             NextLessonId,
    List<CourseMaterialDto> Materials
);

public record CourseMaterialDto(
    int              Id,
    string           FileName,
    string           FileUrl,
    MaterialFileType FileType,
    int?             FileSizeKB
);

// ── Dropdown options cho course builder ──────────────────────────────────────

public record ClassOptionDto(int Id, string ClassName, string YearName, bool YearActive);

public record QuizExamOptionDto(int Id, string Title, string SubjectName, QuizExamStatus Status);

/// <summary>Heartbeat từ video player (gửi mỗi 15–30s).</summary>
public record VideoHeartbeatRequest(
    int PositionSec,
    int WatchedDeltaSec
);

public record VideoHeartbeatResponse(
    int     WatchedSec,
    bool    LessonCompleted,
    decimal CourseProgressPercent
);

public record LessonCompletionResultDto(
    bool    LessonCompleted,
    decimal CourseProgressPercent,
    bool    CourseCompleted
);
