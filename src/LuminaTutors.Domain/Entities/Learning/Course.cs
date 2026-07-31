using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Learning;

// ═════════════════════════════════════════════════════════════════════════════
//  E-LEARNING: Course → CourseModule → CourseLesson  +  Enrollment & Progress
//
//  Course là KHO NỘI DUNG độc lập của trường (mô hình hybrid):
//  • Gán cho lớp (ClassCourseAssignment)  → auto ghi danh học sinh của lớp.
//  • Ghi danh lẻ (CourseEnrollment)       → tự học / bán khóa B2C sau này.
//  Khác với Lesson (giáo án gắn SubjectAssignment của lớp live), Course tái sử
//  dụng được qua nhiều năm học / nhiều lớp.
// ═════════════════════════════════════════════════════════════════════════════

// ─── Course ───────────────────────────────────────────────────────────────────

public class Course : TenantEntity
{
    public string  Title        { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string? ThumbnailUrl { get; set; }

    public int? SubjectId    { get; set; }   // null = khóa liên môn / kỹ năng
    public int? GradeLevelId { get; set; }   // null = mọi khối

    public CourseStatus Status      { get; set; } = CourseStatus.Draft;
    public DateTime?    PublishedAt { get; set; }

    /// <summary>true = học sinh phải hoàn thành bài trước mới mở bài sau.</summary>
    public bool IsSequential { get; set; } = false;

    public int CreatedByUserId { get; set; }

    // Navigation
    public School      School       { get; set; } = null!;
    public Subject?    Subject      { get; set; }
    public GradeLevel? GradeLevel   { get; set; }
    public User        CreatedBy    { get; set; } = null!;
    public ICollection<CourseModule>          Modules          { get; set; } = [];
    public ICollection<ClassCourseAssignment> ClassAssignments { get; set; } = [];
    public ICollection<CourseEnrollment>      Enrollments      { get; set; } = [];
}

// ─── CourseModule (chương) ────────────────────────────────────────────────────

public class CourseModule : AuditableEntity
{
    public int     CourseId    { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int     SortOrder   { get; set; } = 0;

    /// <summary>Drip tương đối: mở khóa sau N ngày kể từ khi học sinh ghi danh (null = mở ngay).</summary>
    public int? UnlockAfterDays { get; set; }

    /// <summary>Drip tuyệt đối: chỉ mở từ thời điểm này (null = không giới hạn). Nếu đặt cả hai, phải thỏa cả hai.</summary>
    public DateTime? AvailableFrom { get; set; }

    // ── Khung PPCT Việt Nam (GDPT 2018) ──────────────────────────────────────
    /// <summary>Học kỳ theo phân phối chương trình (1/2; null = không gắn học kỳ).</summary>
    public byte? SemesterNo { get; set; }

    /// <summary>Tuần PPCT bắt đầu dạy chủ đề/chương này (1–35; null = không gắn tuần).</summary>
    public byte? StartWeek { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<CourseLesson> Lessons { get; set; } = [];
}

// ─── CourseLesson (bài học) ───────────────────────────────────────────────────

public class CourseLesson : AuditableEntity
{
    public int    ModuleId  { get; set; }
    public string Title     { get; set; } = string.Empty;
    public int    SortOrder { get; set; } = 0;

    public CourseLessonType ContentType { get; set; } = CourseLessonType.Article;

    /// <summary>Nội dung bài đọc (ContentType = Article, hoặc mô tả kèm video).</summary>
    public string? ContentHtml { get; set; }

    /// <summary>URL video (file HLS/MP4 hoặc YouTube embed) khi ContentType = Video.</summary>
    public string? VideoUrl         { get; set; }
    public int?    VideoDurationSec { get; set; }

    /// <summary>Đề trắc nghiệm liên kết khi ContentType = Quiz.</summary>
    public int? QuizExamId { get; set; }

    /// <summary>% thời lượng video tối thiểu phải xem để tính hoàn thành (mặc định 90%).</summary>
    public byte MinWatchPercent { get; set; } = 90;

    /// <summary>Cho xem thử không cần ghi danh (phục vụ trang giới thiệu khóa sau này).</summary>
    public bool IsPreviewable { get; set; } = false;

    public bool IsPublished { get; set; } = false;

    // ── Khung bài dạy Việt Nam (GDPT 2018) ───────────────────────────────────
    /// <summary>Yêu cầu cần đạt — hiển thị cho học sinh trước khi học (chuẩn GDPT 2018).</summary>
    public string? Objectives { get; set; }

    /// <summary>Mức độ tư duy: Nhận biết / Thông hiểu / Vận dụng / Vận dụng cao (null = không phân loại).</summary>
    public CognitiveLevel? CognitiveLevel { get; set; }

    /// <summary>Số tiết PPCT tương ứng bài học (mặc định 1 tiết).</summary>
    public byte PeriodCount { get; set; } = 1;

    // Navigation
    public CourseModule Module   { get; set; } = null!;
    public QuizExam?    QuizExam { get; set; }
    public ICollection<CourseLessonMaterial> Materials { get; set; } = [];
}

// ─── CourseLessonMaterial (tài liệu đính kèm bài học) ─────────────────────────

public class CourseLessonMaterial : BaseEntity
{
    public int    CourseLessonId { get; set; }
    public string FileName       { get; set; } = string.Empty;
    public string FileUrl        { get; set; } = string.Empty;
    public MaterialFileType FileType { get; set; }
    public int?   FileSizeKB     { get; set; }
    public byte   SortOrder      { get; set; } = 0;
    public DateTime UploadedAt   { get; set; } = DateTime.UtcNow;

    public CourseLesson CourseLesson { get; set; } = null!;
}

// ─── ClassCourseAssignment (gán khóa học cho lớp) ─────────────────────────────

public class ClassCourseAssignment : TenantEntity
{
    public int  CourseId         { get; set; }
    public int  ClassId          { get; set; }
    public int  AssignedByUserId { get; set; }

    /// <summary>Giới hạn thời gian lớp được truy cập khóa (null = không giới hạn).</summary>
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate   { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public School School     { get; set; } = null!;
    public Course Course     { get; set; } = null!;
    public Class  Class      { get; set; } = null!;
    public User   AssignedBy { get; set; } = null!;
    public ICollection<CourseEnrollment> Enrollments { get; set; } = [];
}

// ─── CourseEnrollment (ghi danh của 1 học sinh vào 1 khóa) ────────────────────

public class CourseEnrollment : TenantEntity
{
    public int CourseId  { get; set; }
    public int StudentId { get; set; }

    public CourseEnrollmentSource Source { get; set; } = CourseEnrollmentSource.Manual;

    /// <summary>Nguồn gốc nếu được ghi danh qua gán lớp.</summary>
    public int? ClassCourseAssignmentId { get; set; }

    public CourseEnrollmentStatus Status { get; set; } = CourseEnrollmentStatus.Active;

    // ── Cache tiến độ (denormalized — service tính lại mỗi khi có bài hoàn thành) ──
    public decimal ProgressPercent      { get; set; } = 0M;     // 0–100
    public int     CompletedLessonCount { get; set; } = 0;

    public DateTime? StartedAt      { get; set; }   // lần đầu mở bài học
    public DateTime? LastAccessedAt { get; set; }
    public int?      LastLessonId   { get; set; }   // resume point
    public DateTime? CompletedAt    { get; set; }

    // Navigation
    public School School  { get; set; } = null!;
    public Course Course  { get; set; } = null!;
    public User   Student { get; set; } = null!;
    public ClassCourseAssignment? ClassAssignment { get; set; }
    public CourseLesson?          LastLesson      { get; set; }
    public ICollection<LessonProgress> LessonProgresses { get; set; } = [];
}

// ─── LessonProgress (tiến độ 1 học sinh trên 1 bài học) ───────────────────────

/// <summary>
/// Row được tạo lazy khi học sinh mở bài lần đầu (không tồn tại = NotStarted).
/// Unique (EnrollmentId, CourseLessonId). Bảng hot-path: heartbeat video ghi vào đây.
/// </summary>
public class LessonProgress : TenantEntity
{
    public int EnrollmentId   { get; set; }
    public int CourseLessonId { get; set; }

    public ProgressStatus Status { get; set; } = ProgressStatus.InProgress;

    /// <summary>Vị trí xem cuối (giây) — để resume video.</summary>
    public int LastPositionSec { get; set; } = 0;

    /// <summary>Tổng số giây đã xem (server clamp chống tua/gian lận).</summary>
    public int WatchedSec { get; set; } = 0;

    /// <summary>Tổng thời gian học trên bài (mọi loại nội dung).</summary>
    public int TimeSpentSec { get; set; } = 0;

    /// <summary>Bằng chứng hoàn thành bài Quiz (attempt đã nộp).</summary>
    public int? QuizAttemptId { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation
    public School           School     { get; set; } = null!;
    public CourseEnrollment Enrollment { get; set; } = null!;
    public CourseLesson     Lesson     { get; set; } = null!;
    public StudentQuizAttempt? QuizAttempt { get; set; }
}
