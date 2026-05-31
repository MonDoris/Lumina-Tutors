using LuminaTutors.Application.DTOs.Homework;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class HomeworkService : IHomeworkService
{
    private readonly IUnitOfWork                 _uow;
    private readonly ILogger<HomeworkService>    _logger;
    private const    string                      UploadRoot = "wwwroot/uploads/homework";

    public HomeworkService(IUnitOfWork uow, ILogger<HomeworkService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ══ TEACHER ══════════════════════════════════════════════════════════════

    public async Task<Result<IReadOnlyList<SubjectAssignmentOptionDto>>> GetTeacherSubjectAssignmentsAsync(
        int schoolId, int teacherId, CancellationToken ct = default)
    {
        var list = await _uow.SubjectAssignments.FindAsync(
            sa => sa.SchoolId == schoolId && sa.TeacherId == teacherId,
            q  => q.Include(sa => sa.Subject).Include(sa => sa.Class),
            ct);

        var dtos = list.Select(sa => new SubjectAssignmentOptionDto(
            sa.Id,
            sa.Subject?.SubjectName ?? "—",
            sa.Class?.ClassName ?? "—",
            sa.Subject?.Id
        )).OrderBy(x => x.SubjectName).ThenBy(x => x.ClassName).ToList();

        return Result<IReadOnlyList<SubjectAssignmentOptionDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<AssignmentListDto>>> GetTeacherAssignmentsAsync(
        int schoolId, int teacherId, CancellationToken ct = default)
    {
        // Get teacher's SubjectAssignment IDs
        var saIds = (await _uow.SubjectAssignments.FindAsync(
            sa => sa.SchoolId == schoolId && sa.TeacherId == teacherId, ct: ct))
            .Select(sa => sa.Id).ToHashSet();

        var assignments = await _uow.Assignments.FindAsync(
            a => a.SchoolId == schoolId && saIds.Contains(a.SubjectAssignmentId),
            q => q.Include(a => a.SubjectAssignment)
                  .ThenInclude(sa => sa.Subject)
                  .Include(a => a.SubjectAssignment)
                  .ThenInclude(sa => sa.Class)
                  .Include(a => a.Submissions),
            ct);

        // Count students per SubjectAssignment (class)
        var classStudentCounts = new Dictionary<int, int>();
        foreach (var saId in assignments.Select(a => a.SubjectAssignmentId).Distinct())
        {
            var sa = await _uow.SubjectAssignments.FindOneAsync(
                x => x.Id == saId,
                q => q.Include(x => x.Class).ThenInclude(c => c.Enrollments),
                ct);
            classStudentCounts[saId] = sa?.Class?.Enrollments?.Count ?? 0;
        }

        var dtos = assignments
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssignmentListDto(
                a.Id,
                a.Title,
                a.SubjectAssignment?.Subject?.SubjectName ?? "—",
                a.SubjectAssignment?.Class?.ClassName    ?? "—",
                a.AssignmentType,
                a.MaxScore,
                a.DueDate,
                a.IsPublished,
                classStudentCounts.GetValueOrDefault(a.SubjectAssignmentId),
                a.Submissions.Count(s => s.SubmissionStatus >= SubmissionStatus.Submitted),
                a.Submissions.Count(s => s.SubmissionStatus == SubmissionStatus.Graded)
            )).ToList();

        return Result<IReadOnlyList<AssignmentListDto>>.Success(dtos);
    }

    public async Task<Result<AssignmentDetailDto>> GetAssignmentDetailAsync(
        int schoolId, int assignmentId, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId,
            q => q.Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Subject)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Class)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Teacher)
                  .Include(x => x.Attachments),
            ct);

        if (a is null) return Result<AssignmentDetailDto>.Failure("Không tìm thấy bài tập.");

        return Result<AssignmentDetailDto>.Success(MapDetail(a));
    }

    public async Task<Result<int>> CreateAssignmentAsync(
        int schoolId, int teacherId, CreateAssignmentRequest req, CancellationToken ct = default)
    {
        // Verify SubjectAssignment belongs to this teacher
        var sa = await _uow.SubjectAssignments.FindOneAsync(
            x => x.Id == req.SubjectAssignmentId && x.TeacherId == teacherId && x.SchoolId == schoolId, ct: ct);
        if (sa is null)
            return Result<int>.Failure("Phân công giảng dạy không hợp lệ.");

        var entity = new Assignment
        {
            SchoolId             = schoolId,
            SubjectAssignmentId  = req.SubjectAssignmentId,
            Title                = req.Title.Trim(),
            Instructions         = req.Instructions?.Trim(),
            AssignmentType       = req.AssignmentType,
            MaxScore             = req.MaxScore,
            DueDate              = req.DueDate,
            AllowLateSubmission  = req.AllowLateSubmission,
            LatePenaltyPercent   = req.LatePenaltyPercent,
            IsPublished          = req.IsPublished,
            PublishedAt          = req.IsPublished ? DateTime.UtcNow : null
        };

        await _uow.Assignments.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAssignmentAsync(
        int schoolId, int assignmentId, UpdateAssignmentRequest req, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId, ct: ct);
        if (a is null) return Result.Failure("Không tìm thấy bài tập.");

        a.Title               = req.Title.Trim();
        a.Instructions        = req.Instructions?.Trim();
        a.AssignmentType      = req.AssignmentType;
        a.MaxScore            = req.MaxScore;
        a.DueDate             = req.DueDate;
        a.AllowLateSubmission = req.AllowLateSubmission;
        a.LatePenaltyPercent  = req.LatePenaltyPercent;

        if (!a.IsPublished && req.IsPublished)
            a.PublishedAt = DateTime.UtcNow;
        a.IsPublished = req.IsPublished;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAssignmentAsync(
        int schoolId, int assignmentId, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId, ct: ct);
        if (a is null) return Result.Failure("Không tìm thấy bài tập.");

        _uow.Assignments.Remove(a);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AttachmentDto>> AddAttachmentAsync(
        int assignmentId, IFormFile file, CancellationToken ct = default)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), UploadRoot, assignmentId.ToString());
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(file.FileName);
        var saved    = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, saved);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(fs, ct);

        var url    = $"/uploads/homework/{assignmentId}/{saved}";
        var sizeKb = (int)(file.Length / 1024);

        var att = new AssignmentAttachment
        {
            AssignmentId = assignmentId,
            FileName     = file.FileName,
            FileUrl      = url,
            FileType     = ext.TrimStart('.').ToUpper(),
            FileSizeKB   = sizeKb
        };
        await _uow.AssignmentAttachments.AddAsync(att, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<AttachmentDto>.Success(new AttachmentDto(att.Id, att.FileName, att.FileUrl, att.FileType, att.FileSizeKB));
    }

    public async Task<Result> DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default)
    {
        var att = await _uow.AssignmentAttachments.GetByIdAsync(attachmentId, ct);
        if (att is null) return Result.Failure("File không tồn tại.");
        _uow.AssignmentAttachments.Remove(att);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AssignmentStatsDto>> GetStatsAsync(
        int schoolId, int assignmentId, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId,
            q => q.Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Subject)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Class)
                      .ThenInclude(c => c.Enrollments).ThenInclude(e => e.Student)
                          .ThenInclude(s => s.StudentProfile)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Teacher)
                  .Include(x => x.Attachments)
                  .Include(x => x.Submissions).ThenInclude(s => s.Student)
                      .ThenInclude(s => s.StudentProfile)
                  .Include(x => x.Submissions).ThenInclude(s => s.Files),
            ct);

        if (a is null) return Result<AssignmentStatsDto>.Failure("Không tìm thấy bài tập.");

        var allStudents = a.SubjectAssignment?.Class?.Enrollments?
            .Select(e => e.Student).Where(s => s != null).ToList() ?? new();

        var submittedStudentIds = a.Submissions
            .Where(s => s.SubmissionStatus >= SubmissionStatus.Submitted)
            .Select(s => s.StudentId).ToHashSet();

        var notSubmitted = allStudents
            .Where(s => !submittedStudentIds.Contains(s!.Id))
            .Select(s => new StudentNotSubmittedDto(s!.Id, s.FullName, s.StudentProfile?.StudentCode ?? "—"))
            .OrderBy(s => s.StudentName).ToList();

        var submissions = a.Submissions
            .Where(s => s.SubmissionStatus >= SubmissionStatus.Submitted)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionRowDto(
                s.Id, s.StudentId, s.Student?.FullName ?? "—",
                s.Student?.StudentProfile?.StudentCode ?? "—",
                s.SubmissionStatus, s.SubmittedAt, s.IsLate, s.Score, s.Feedback,
                s.Files.Select(f => new SubmissionFileDto(f.Id, f.FileName, f.FileUrl, f.FileType, f.FileSizeKB)).ToList()
            )).ToList();

        var detail = MapDetail(a);
        var stats  = new AssignmentStatsDto(
            detail,
            TotalStudents:    allStudents.Count,
            SubmittedCount:   submittedStudentIds.Count,
            LateCount:        a.Submissions.Count(s => s.IsLate && s.SubmissionStatus >= SubmissionStatus.Submitted),
            GradedCount:      a.Submissions.Count(s => s.SubmissionStatus == SubmissionStatus.Graded),
            NotSubmittedCount: notSubmitted.Count,
            submissions, notSubmitted
        );

        return Result<AssignmentStatsDto>.Success(stats);
    }

    public async Task<Result> GradeSubmissionAsync(
        int submissionId, GradeSubmissionRequest req, CancellationToken ct = default)
    {
        var sub = await _uow.AssignmentSubmissions.GetByIdAsync(submissionId, ct);
        if (sub is null) return Result.Failure("Không tìm thấy bài nộp.");

        sub.Score            = req.Score;
        sub.Feedback         = req.Feedback;
        sub.GradedAt         = DateTime.UtcNow;
        sub.SubmissionStatus = SubmissionStatus.Graded;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ══ STUDENT ══════════════════════════════════════════════════════════════

    public async Task<Result<IReadOnlyList<StudentCourseDto>>> GetStudentCoursesAsync(
        int schoolId, int studentId, CancellationToken ct = default)
    {
        // Find classes the student is enrolled in
        var enrollments = await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == studentId && e.Class.SchoolId == schoolId,
            q => q.Include(e => e.Class), ct);

        var classIds = enrollments.Select(e => e.ClassId).ToHashSet();

        // SubjectAssignments for those classes
        var sas = await _uow.SubjectAssignments.FindAsync(
            sa => sa.SchoolId == schoolId && classIds.Contains(sa.ClassId),
            q  => q.Include(sa => sa.Subject)
                   .Include(sa => sa.Class)
                   .Include(sa => sa.Teacher)
                   .Include(sa => sa.Assignments).ThenInclude(a => a.Submissions),
            ct);

        var now  = DateTime.UtcNow;
        var dtos = sas.Select(sa =>
        {
            var published    = sa.Assignments.Where(a => a.IsPublished).ToList();
            var mySubmissions = published.Select(a =>
                a.Submissions.FirstOrDefault(s => s.StudentId == studentId)).ToList();

            var submitted  = mySubmissions.Count(s => s?.SubmissionStatus >= SubmissionStatus.Submitted);
            var pending    = published.Count - submitted;
            var overdue    = published.Count(a =>
                a.DueDate.HasValue && a.DueDate.Value < now &&
                !(a.Submissions.Any(s => s.StudentId == studentId && s.SubmissionStatus >= SubmissionStatus.Submitted)));

            return new StudentCourseDto(
                sa.Id,
                sa.Subject?.SubjectName  ?? "—",
                sa.Subject?.SubjectCode  ?? "—",
                sa.Class?.ClassName      ?? "—",
                sa.Teacher?.FullName     ?? "—",
                published.Count,
                submitted,
                pending,
                overdue
            );
        }).OrderBy(c => c.SubjectName).ToList();

        return Result<IReadOnlyList<StudentCourseDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<StudentAssignmentDto>>> GetStudentAssignmentsAsync(
        int schoolId, int studentId, int subjectAssignmentId, CancellationToken ct = default)
    {
        var assignments = await _uow.Assignments.FindAsync(
            a => a.SchoolId == schoolId && a.SubjectAssignmentId == subjectAssignmentId && a.IsPublished,
            q => q.Include(a => a.SubjectAssignment).ThenInclude(sa => sa.Subject)
                  .Include(a => a.SubjectAssignment).ThenInclude(sa => sa.Class)
                  .Include(a => a.SubjectAssignment).ThenInclude(sa => sa.Teacher)
                  .Include(a => a.Attachments)
                  .Include(a => a.Submissions).ThenInclude(s => s.Files),
            ct);

        var dtos = assignments
            .OrderByDescending(a => a.DueDate ?? a.CreatedAt)
            .Select(a => MapStudentAssignment(a, studentId))
            .ToList();

        return Result<IReadOnlyList<StudentAssignmentDto>>.Success(dtos);
    }

    public async Task<Result<StudentAssignmentDto>> GetStudentAssignmentDetailAsync(
        int schoolId, int studentId, int assignmentId, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId && x.IsPublished,
            q => q.Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Subject)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Class)
                  .Include(x => x.SubjectAssignment).ThenInclude(sa => sa.Teacher)
                  .Include(x => x.Attachments)
                  .Include(x => x.Submissions).ThenInclude(s => s.Files),
            ct);

        if (a is null) return Result<StudentAssignmentDto>.Failure("Không tìm thấy bài tập.");
        return Result<StudentAssignmentDto>.Success(MapStudentAssignment(a, studentId));
    }

    public async Task<Result<int>> SubmitAssignmentAsync(
        int schoolId, int studentId, int assignmentId,
        string? answerText, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        var a = await _uow.Assignments.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == assignmentId && x.IsPublished, ct: ct);
        if (a is null) return Result<int>.Failure("Không tìm thấy bài tập.");

        var now  = DateTime.UtcNow;
        var late = a.DueDate.HasValue && now > a.DueDate.Value;
        if (late && !a.AllowLateSubmission)
            return Result<int>.Failure("Bài tập đã quá hạn và không cho phép nộp muộn.");

        // Find or create submission
        var sub = await _uow.AssignmentSubmissions.FindOneAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId,
            q => q.Include(s => s.Files), ct);

        if (sub is null)
        {
            sub = new AssignmentSubmission
            {
                AssignmentId     = assignmentId,
                StudentId        = studentId,
                SubmissionStatus = SubmissionStatus.Draft
            };
            await _uow.AssignmentSubmissions.AddAsync(sub, ct);
            await _uow.SaveChangesAsync(ct);
        }

        sub.AnswerText       = answerText?.Trim();
        sub.SubmittedAt      = now;
        sub.IsLate           = late;
        sub.SubmissionStatus = SubmissionStatus.Submitted;

        // Save uploaded files
        var fileList = files.ToList();
        if (fileList.Any())
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/uploads/submissions", sub.Id.ToString());
            Directory.CreateDirectory(dir);

            foreach (var f in fileList)
            {
                if (f.Length == 0) continue;
                var ext   = Path.GetExtension(f.FileName);
                var saved = $"{Guid.NewGuid()}{ext}";
                await using var fs = new FileStream(Path.Combine(dir, saved), FileMode.Create);
                await f.CopyToAsync(fs, ct);

                sub.Files.Add(new SubmissionFile
                {
                    FileName   = f.FileName,
                    FileUrl    = $"/uploads/submissions/{sub.Id}/{saved}",
                    FileType   = ext.TrimStart('.').ToUpper(),
                    FileSizeKB = (int)(f.Length / 1024)
                });
            }
        }

        await _uow.SaveChangesAsync(ct);
        return Result<int>.Success(sub.Id);
    }

    public async Task<Result> DeleteSubmissionFileAsync(int fileId, int studentId, CancellationToken ct = default)
    {
        var file = await _uow.SubmissionFiles.FindOneAsync(
            f => f.Id == fileId && f.Submission!.StudentId == studentId,
            q => q.Include(f => f.Submission), ct);

        if (file is null) return Result.Failure("File không tồn tại.");

        // Delete physical file
        var physical = Path.Combine(Directory.GetCurrentDirectory(), file.FileUrl.TrimStart('/'));
        if (File.Exists(physical)) File.Delete(physical);

        _uow.SubmissionFiles.Remove(file);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ══ Private helpers ═══════════════════════════════════════════════════════

    private static AssignmentDetailDto MapDetail(Assignment a) => new(
        a.Id, a.SubjectAssignmentId, a.Title, a.Instructions, a.AssignmentType,
        a.MaxScore, a.DueDate, a.AllowLateSubmission, a.LatePenaltyPercent, a.IsPublished,
        a.SubjectAssignment?.Subject?.SubjectName ?? "—",
        a.SubjectAssignment?.Class?.ClassName     ?? "—",
        a.SubjectAssignment?.Teacher?.FullName    ?? "—",
        a.Attachments.Select(at => new AttachmentDto(at.Id, at.FileName, at.FileUrl, at.FileType, at.FileSizeKB)).ToList()
    );

    private static StudentAssignmentDto MapStudentAssignment(Assignment a, int studentId)
    {
        var mySub = a.Submissions.FirstOrDefault(s => s.StudentId == studentId);
        SubmissionRowDto? mySubDto = mySub is null ? null : new(
            mySub.Id, mySub.StudentId, "", "",
            mySub.SubmissionStatus, mySub.SubmittedAt, mySub.IsLate, mySub.Score, mySub.Feedback,
            mySub.Files.Select(f => new SubmissionFileDto(f.Id, f.FileName, f.FileUrl, f.FileType, f.FileSizeKB)).ToList()
        );

        return new StudentAssignmentDto(
            a.Id, a.Title, a.Instructions, a.AssignmentType, a.MaxScore, a.DueDate, a.AllowLateSubmission,
            a.SubjectAssignment?.Subject?.SubjectName ?? "—",
            a.SubjectAssignment?.Class?.ClassName     ?? "—",
            a.SubjectAssignment?.Teacher?.FullName    ?? "—",
            a.Attachments.Select(at => new AttachmentDto(at.Id, at.FileName, at.FileUrl, at.FileType, at.FileSizeKB)).ToList(),
            mySubDto
        );
    }
}
