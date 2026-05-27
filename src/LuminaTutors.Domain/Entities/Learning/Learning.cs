using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Grading;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Learning;

// ─── Lesson ───────────────────────────────────────────────────────────────────

public class Lesson : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SubjectAssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentHtml { get; set; }
    public LessonType LessonType { get; set; } = LessonType.Lecture;
    public bool Is3DEnabled { get; set; } = false;
    public string? Lab3DConfig { get; set; }    // JSON: { subject, experiment, mode }
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    public School School { get; set; } = null!;
    public SubjectAssignment SubjectAssignment { get; set; } = null!;
    public ICollection<LessonMaterial> Materials { get; set; } = [];
}

// ─── LessonMaterial ───────────────────────────────────────────────────────────

public class LessonMaterial : BaseEntity
{
    public int LessonId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MaterialFileType FileType { get; set; }
    public int? FileSizeKB { get; set; }
    public byte SortOrder { get; set; } = 0;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Lesson Lesson { get; set; } = null!;
}

// ─── QuestionBank ─────────────────────────────────────────────────────────────

public class QuestionBank : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SubjectId { get; set; }
    public int CreatedByTeacherId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Medium;
    public int? GradeLevelId { get; set; }
    public string? ChapterTag { get; set; }
    public string? ExplanationText { get; set; }
    public bool IsApproved { get; set; } = false;

    public School School { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public User CreatedByTeacher { get; set; } = null!;
    public GradeLevel? GradeLevel { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = [];
}

// ─── QuestionOption ───────────────────────────────────────────────────────────

public class QuestionOption : BaseEntity
{
    public int QuestionId { get; set; }
    public char OptionLabel { get; set; }   // A, B, C, D
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;

    public QuestionBank Question { get; set; } = null!;
}

// ─── Assignment ───────────────────────────────────────────────────────────────

public class Assignment : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SubjectAssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public int? GradeCategoryId { get; set; }
    public decimal MaxScore { get; set; } = 10M;
    public DateTime? DueDate { get; set; }
    public bool AllowLateSubmission { get; set; } = false;
    public byte LatePenaltyPercent { get; set; } = 0;
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    public School School { get; set; } = null!;
    public SubjectAssignment SubjectAssignment { get; set; } = null!;
    public GradeCategory? GradeCategory { get; set; }
    public ICollection<AssignmentSubmission> Submissions { get; set; } = [];
}

// ─── AssignmentSubmission ─────────────────────────────────────────────────────

public class AssignmentSubmission : AuditableEntity
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsLate { get; set; } = false;
    public string? AnswerText { get; set; }
    public decimal? Score { get; set; }
    public DateTime? GradedAt { get; set; }
    public int? GradedByUserId { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus SubmissionStatus { get; set; } = SubmissionStatus.Draft;

    public Assignment Assignment { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User? GradedBy { get; set; }
    public ICollection<SubmissionFile> Files { get; set; } = [];
}

// ─── SubmissionFile ───────────────────────────────────────────────────────────

public class SubmissionFile : BaseEntity
{
    public int SubmissionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int? FileSizeKB { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public AssignmentSubmission Submission { get; set; } = null!;
}

// ─── VirtualLabSession ────────────────────────────────────────────────────────

/// <summary>
/// Represents an active 3D virtual lab session a teacher starts and students
/// join via a short access code. The Three.js scene is driven by SceneType.
/// SubjectTag: "chemistry" | "physics" | "biology"
/// SceneType : "titration" | "pendulum" | "cell"
/// </summary>
public class VirtualLabSession : TenantEntity
{
    public int    TeacherId       { get; set; }
    public string SessionName     { get; set; } = string.Empty;
    public string SessionCode     { get; set; } = string.Empty;  // 6-char uppercase
    public string SubjectTag      { get; set; } = string.Empty;
    public string SceneType       { get; set; } = string.Empty;
    public bool   IsActive        { get; set; } = true;
    public int    MaxParticipants { get; set; } = 40;

    // Navigation
    public School School  { get; set; } = null!;
    public User   Teacher { get; set; } = null!;
}
