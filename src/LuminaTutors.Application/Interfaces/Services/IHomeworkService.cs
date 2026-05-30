using LuminaTutors.Application.DTOs.Homework;
using LuminaTutors.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IHomeworkService
{
    // ── Teacher ───────────────────────────────────────────────────────────────

    /// <summary>Danh sách bài tập của giáo viên theo schoolId + teacherId.</summary>
    Task<Result<IReadOnlyList<AssignmentListDto>>> GetTeacherAssignmentsAsync(
        int schoolId, int teacherId, CancellationToken ct = default);

    /// <summary>Danh sách SubjectAssignment (lớp-môn) do giáo viên phụ trách.</summary>
    Task<Result<IReadOnlyList<SubjectAssignmentOptionDto>>> GetTeacherSubjectAssignmentsAsync(
        int schoolId, int teacherId, CancellationToken ct = default);

    /// <summary>Chi tiết bài tập.</summary>
    Task<Result<AssignmentDetailDto>> GetAssignmentDetailAsync(
        int schoolId, int assignmentId, CancellationToken ct = default);

    /// <summary>Tạo bài tập mới.</summary>
    Task<Result<int>> CreateAssignmentAsync(
        int schoolId, int teacherId, CreateAssignmentRequest req, CancellationToken ct = default);

    /// <summary>Cập nhật bài tập.</summary>
    Task<Result> UpdateAssignmentAsync(
        int schoolId, int assignmentId, UpdateAssignmentRequest req, CancellationToken ct = default);

    /// <summary>Xóa bài tập.</summary>
    Task<Result> DeleteAssignmentAsync(
        int schoolId, int assignmentId, CancellationToken ct = default);

    /// <summary>Đính kèm file vào bài tập (teacher).</summary>
    Task<Result<AttachmentDto>> AddAttachmentAsync(
        int assignmentId, IFormFile file, CancellationToken ct = default);

    /// <summary>Xóa file đính kèm.</summary>
    Task<Result> DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default);

    /// <summary>Thống kê nộp bài của cả lớp.</summary>
    Task<Result<AssignmentStatsDto>> GetStatsAsync(
        int schoolId, int assignmentId, CancellationToken ct = default);

    /// <summary>Giáo viên chấm điểm 1 bài nộp.</summary>
    Task<Result> GradeSubmissionAsync(
        int submissionId, GradeSubmissionRequest req, CancellationToken ct = default);

    // ── Student ───────────────────────────────────────────────────────────────

    /// <summary>Danh sách môn học (course folders) của học sinh.</summary>
    Task<Result<IReadOnlyList<StudentCourseDto>>> GetStudentCoursesAsync(
        int schoolId, int studentId, CancellationToken ct = default);

    /// <summary>Bài tập trong 1 môn của học sinh.</summary>
    Task<Result<IReadOnlyList<StudentAssignmentDto>>> GetStudentAssignmentsAsync(
        int schoolId, int studentId, int subjectAssignmentId, CancellationToken ct = default);

    /// <summary>Chi tiết 1 bài tập + bài nộp của học sinh.</summary>
    Task<Result<StudentAssignmentDto>> GetStudentAssignmentDetailAsync(
        int schoolId, int studentId, int assignmentId, CancellationToken ct = default);

    /// <summary>Nộp bài (text + files).</summary>
    Task<Result<int>> SubmitAssignmentAsync(
        int schoolId, int studentId, int assignmentId,
        string? answerText, IEnumerable<IFormFile> files, CancellationToken ct = default);

    /// <summary>Xóa file bài nộp.</summary>
    Task<Result> DeleteSubmissionFileAsync(int fileId, int studentId, CancellationToken ct = default);
}
