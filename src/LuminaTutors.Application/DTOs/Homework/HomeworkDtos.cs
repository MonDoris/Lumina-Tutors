using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LuminaTutors.Application.DTOs.Homework;

// ── Teacher: Assignment list item ─────────────────────────────────────────────

public record AssignmentListDto(
    int              Id,
    string           Title,
    string           SubjectName,
    string           ClassName,
    AssignmentType   AssignmentType,
    decimal          MaxScore,
    DateTime?        DueDate,
    bool             IsPublished,
    int              TotalStudents,
    int              SubmittedCount,
    int              GradedCount
);

// ── Teacher: Assignment detail ────────────────────────────────────────────────

public record AssignmentDetailDto(
    int              Id,
    int              SubjectAssignmentId,
    string           Title,
    string?          Instructions,
    AssignmentType   AssignmentType,
    decimal          MaxScore,
    DateTime?        DueDate,
    bool             AllowLateSubmission,
    byte             LatePenaltyPercent,
    bool             IsPublished,
    string           SubjectName,
    string           ClassName,
    string           TeacherName,
    List<AttachmentDto> Attachments
);

// ── Teacher: Submission row ───────────────────────────────────────────────────

public record SubmissionRowDto(
    int               SubmissionId,
    int               StudentId,
    string            StudentName,
    string            StudentCode,
    SubmissionStatus  Status,
    DateTime?         SubmittedAt,
    bool              IsLate,
    decimal?          Score,
    string?           Feedback,
    List<SubmissionFileDto> Files
);

// ── Teacher: Statistics ───────────────────────────────────────────────────────

public record AssignmentStatsDto(
    AssignmentDetailDto Assignment,
    int  TotalStudents,
    int  SubmittedCount,
    int  LateCount,
    int  GradedCount,
    int  NotSubmittedCount,
    List<SubmissionRowDto> Submissions,
    List<StudentNotSubmittedDto> NotSubmitted
);

public record StudentNotSubmittedDto(int StudentId, string StudentName, string StudentCode);

// ── Shared DTOs ───────────────────────────────────────────────────────────────

public record AttachmentDto(int Id, string FileName, string FileUrl, string FileType, int? FileSizeKB);
public record SubmissionFileDto(int Id, string FileName, string FileUrl, string FileType, int? FileSizeKB);

// ── Student: Course (subject folder) ─────────────────────────────────────────

public record StudentCourseDto(
    int    SubjectAssignmentId,
    string SubjectName,
    string SubjectCode,
    string ClassName,
    string TeacherName,
    int    TotalAssignments,
    int    SubmittedCount,
    int    PendingCount,
    int    OverdueCount
);

// ── Student: Assignment card ──────────────────────────────────────────────────

public record StudentAssignmentDto(
    int              Id,
    string           Title,
    string?          Instructions,
    AssignmentType   AssignmentType,
    decimal          MaxScore,
    DateTime?        DueDate,
    bool             AllowLateSubmission,
    string           SubjectName,
    string           ClassName,
    string           TeacherName,
    List<AttachmentDto> Attachments,
    // Student's own submission (null if not submitted)
    SubmissionRowDto? MySubmission
);

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateAssignmentRequest(
    int            SubjectAssignmentId,
    string         Title,
    string?        Instructions,
    AssignmentType AssignmentType,
    decimal        MaxScore,
    DateTime?      DueDate,
    bool           AllowLateSubmission,
    byte           LatePenaltyPercent,
    bool           IsPublished
);

public record UpdateAssignmentRequest(
    string         Title,
    string?        Instructions,
    AssignmentType AssignmentType,
    decimal        MaxScore,
    DateTime?      DueDate,
    bool           AllowLateSubmission,
    byte           LatePenaltyPercent,
    bool           IsPublished
);

public record GradeSubmissionRequest(
    decimal Score,
    string? Feedback
);

public record SubjectAssignmentOptionDto(int Id, string SubjectName, string ClassName, int? PrimarySubjectId = null);
