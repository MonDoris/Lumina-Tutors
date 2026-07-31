using LuminaTutors.Application.DTOs.Course;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

/// <summary>
/// E-Learning: quản trị khóa học và theo dõi tiến độ.
/// Quy tắc hoàn thành bài:
///  • Video   → WatchedSec ≥ MinWatchPercent% × VideoDurationSec (heartbeat có clamp chống tua).
///  • Article → học sinh bấm "Hoàn thành".
///  • Quiz    → có StudentQuizAttempt đã nộp cho QuizExam liên kết.
/// ProgressPercent của enrollment = số bài Completed / tổng bài IsPublished của khóa.
/// </summary>
public sealed class CourseService : ICourseService
{
    private readonly IUnitOfWork            _uow;
    private readonly ILogger<CourseService> _logger;

    /// <summary>Trần mỗi lần cộng dồn WatchedSec (chống client gửi delta ảo).</summary>
    private const int MaxHeartbeatDeltaSec = 300;

    public CourseService(IUnitOfWork uow, ILogger<CourseService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ══ Teacher/Admin: Course CRUD ════════════════════════════════════════════

    public async Task<Result<IReadOnlyList<CourseListItemDto>>> GetCoursesAsync(
        int schoolId, int? createdByUserId = null, CancellationToken ct = default)
    {
        var query = _uow.Courses.AsQueryable().Where(c => c.SchoolId == schoolId);
        if (createdByUserId.HasValue)
            query = query.Where(c => c.CreatedByUserId == createdByUserId.Value);

        var list = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new CourseListItemDto(
                c.Id,
                c.Title,
                c.ThumbnailUrl,
                c.Subject != null ? c.Subject.SubjectName : null,
                c.GradeLevel != null ? c.GradeLevel.GradeName : null,
                c.Status,
                c.IsSequential,
                c.Modules.Count,
                c.Modules.SelectMany(m => m.Lessons).Count(),
                c.Enrollments.Count(e => e.Status != CourseEnrollmentStatus.Dropped),
                c.Enrollments.Where(e => e.Status != CourseEnrollmentStatus.Dropped)
                             .Average(e => (decimal?)e.ProgressPercent) ?? 0M,
                c.UpdatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<CourseListItemDto>>.Success(list);
    }

    public async Task<Result<CourseDetailDto>> GetCourseDetailAsync(
        int schoolId, int courseId, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FindOneAsync(
            c => c.Id == courseId && c.SchoolId == schoolId,
            q => q.Include(c => c.Subject)
                  .Include(c => c.GradeLevel)
                  .Include(c => c.CreatedBy)
                  .Include(c => c.Modules).ThenInclude(m => m.Lessons).ThenInclude(l => l.Materials),
            ct);

        if (course is null)
            return Result<CourseDetailDto>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        var modules = course.Modules
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
            .Select(m => new CourseModuleDto(
                m.Id, m.Title, m.Description, m.SortOrder, m.UnlockAfterDays, m.AvailableFrom,
                m.SemesterNo, m.StartWeek,
                m.Lessons.OrderBy(l => l.SortOrder).ThenBy(l => l.Id)
                    .Select(l => new CourseLessonDto(
                        l.Id, l.Title, l.SortOrder, l.ContentType, l.ContentHtml, l.VideoUrl, l.VideoDurationSec,
                        l.QuizExamId, l.MinWatchPercent, l.IsPreviewable, l.IsPublished,
                        l.Objectives, l.CognitiveLevel, l.PeriodCount, l.Materials.Count))
                    .ToList()))
            .ToList();

        var dto = new CourseDetailDto(
            course.Id, course.Title, course.Description, course.ThumbnailUrl,
            course.SubjectId, course.GradeLevelId,
            course.Subject?.SubjectName, course.GradeLevel?.GradeName,
            course.Status, course.IsSequential, course.PublishedAt,
            course.CreatedBy?.FullName ?? "—", modules);

        return Result<CourseDetailDto>.Success(dto);
    }

    public async Task<Result<int>> CreateCourseAsync(
        int schoolId, int userId, CreateCourseRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<int>.Failure("Tên khóa học không được để trống.", "TITLE_REQUIRED");

        if (request.SubjectId.HasValue &&
            !await _uow.Subjects.AnyAsync(s => s.Id == request.SubjectId.Value && s.SchoolId == schoolId, ct))
            return Result<int>.Failure("Môn học không hợp lệ.", "SUBJECT_INVALID");

        if (request.GradeLevelId.HasValue &&
            !await _uow.GradeLevels.AnyAsync(g => g.Id == request.GradeLevelId.Value && g.SchoolId == schoolId, ct))
            return Result<int>.Failure("Khối lớp không hợp lệ.", "GRADELEVEL_INVALID");

        var course = new Course
        {
            SchoolId        = schoolId,
            Title           = request.Title.Trim(),
            Description     = request.Description,
            ThumbnailUrl    = request.ThumbnailUrl,
            SubjectId       = request.SubjectId,
            GradeLevelId    = request.GradeLevelId,
            IsSequential    = request.IsSequential,
            Status          = CourseStatus.Draft,
            CreatedByUserId = userId
        };

        await _uow.Courses.AddAsync(course, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Tạo khóa học {CourseId} '{Title}' (school {SchoolId})", course.Id, course.Title, schoolId);
        return Result<int>.Success(course.Id);
    }

    public async Task<Result> UpdateCourseAsync(
        int schoolId, int courseId, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure("Tên khóa học không được để trống.", "TITLE_REQUIRED");

        course.Title        = request.Title.Trim();
        course.Description  = request.Description;
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.SubjectId    = request.SubjectId;
        course.GradeLevelId = request.GradeLevelId;
        course.IsSequential = request.IsSequential;

        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangeCourseStatusAsync(
        int schoolId, int courseId, CourseStatus status, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        if (status == CourseStatus.Published)
        {
            var hasPublishedLesson = await _uow.CourseLessons.AnyAsync(
                l => l.Module.CourseId == courseId && l.IsPublished, ct);
            if (!hasPublishedLesson)
                return Result.Failure("Khóa học cần ít nhất 1 bài học đã publish trước khi phát hành.", "NO_PUBLISHED_LESSON");

            course.PublishedAt ??= DateTime.UtcNow;
        }

        course.Status = status;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Khóa học {CourseId} chuyển trạng thái {Status}", courseId, status);
        return Result.Success();
    }

    public async Task<Result> DeleteCourseAsync(int schoolId, int courseId, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        if (course.Status != CourseStatus.Draft)
            return Result.Failure("Chỉ xóa được khóa học ở trạng thái Nháp. Hãy dùng Lưu trữ (Archive) thay thế.", "COURSE_NOT_DRAFT");

        if (await _uow.CourseEnrollments.AnyAsync(e => e.CourseId == courseId, ct))
            return Result.Failure("Khóa học đã có học sinh ghi danh — không thể xóa.", "COURSE_HAS_ENROLLMENTS");

        if (await _uow.ClassCourseAssignments.AnyAsync(a => a.CourseId == courseId, ct))
            return Result.Failure("Khóa học đang được gán cho lớp — hãy gỡ gán trước.", "COURSE_ASSIGNED");

        _uow.Courses.Remove(course);   // Modules/Lessons/Materials xóa theo (cascade)
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ══ Teacher/Admin: Module & Lesson ════════════════════════════════════════

    public async Task<Result<int>> SaveModuleAsync(
        int schoolId, SaveModuleRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<int>.Failure("Tên chương không được để trống.", "TITLE_REQUIRED");

        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == request.CourseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result<int>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        CourseModule module;
        if (request.Id.HasValue)
        {
            var existing = await _uow.CourseModules.FirstOrDefaultAsync(
                m => m.Id == request.Id.Value && m.CourseId == request.CourseId, ct);
            if (existing is null)
                return Result<int>.Failure("Không tìm thấy chương.", "MODULE_NOT_FOUND");
            module = existing;
        }
        else
        {
            var maxSort = await _uow.CourseModules.AsQueryable()
                .Where(m => m.CourseId == request.CourseId)
                .MaxAsync(m => (int?)m.SortOrder, ct) ?? -1;
            module = new CourseModule { CourseId = request.CourseId, SortOrder = maxSort + 1 };
            await _uow.CourseModules.AddAsync(module, ct);
        }

        if (request.SemesterNo is not null and not (1 or 2))
            return Result<int>.Failure("Học kỳ chỉ nhận giá trị 1 hoặc 2.", "SEMESTER_INVALID");
        if (request.StartWeek is not null and (< 1 or > 53))
            return Result<int>.Failure("Tuần PPCT phải trong khoảng 1–53.", "WEEK_INVALID");

        module.Title           = request.Title.Trim();
        module.Description     = request.Description;
        module.UnlockAfterDays = request.UnlockAfterDays;
        module.AvailableFrom   = request.AvailableFrom;
        module.SemesterNo      = request.SemesterNo;
        module.StartWeek       = request.StartWeek;

        if (request.Id.HasValue) _uow.CourseModules.Update(module);
        await _uow.SaveChangesAsync(ct);
        return Result<int>.Success(module.Id);
    }

    public async Task<Result> DeleteModuleAsync(int schoolId, int moduleId, CancellationToken ct = default)
    {
        var module = await _uow.CourseModules.FindOneAsync(
            m => m.Id == moduleId && m.Course.SchoolId == schoolId,
            q => q.Include(m => m.Course), ct);
        if (module is null)
            return Result.Failure("Không tìm thấy chương.", "MODULE_NOT_FOUND");

        var hasProgress = await _uow.LessonProgresses.AnyAsync(
            p => p.Lesson.ModuleId == moduleId, ct);
        if (hasProgress)
            return Result.Failure("Đã có học sinh học chương này — hãy ẩn (unpublish) các bài học thay vì xóa.", "MODULE_HAS_PROGRESS");

        _uow.CourseModules.Remove(module);   // Lessons/Materials xóa theo (cascade)
        await _uow.SaveChangesAsync(ct);

        await RecalcCourseEnrollmentsAsync(module.CourseId, ct);
        return Result.Success();
    }

    public async Task<Result> ReorderModulesAsync(
        int schoolId, int courseId, List<int> orderedModuleIds, CancellationToken ct = default)
    {
        var modules = await _uow.CourseModules.FindAsync(
            m => m.CourseId == courseId && m.Course.SchoolId == schoolId, ct: ct);

        if (modules.Count != orderedModuleIds.Count ||
            modules.Select(m => m.Id).Except(orderedModuleIds).Any())
            return Result.Failure("Danh sách chương không khớp với khóa học.", "REORDER_MISMATCH");

        var byId = modules.ToDictionary(m => m.Id);
        for (var i = 0; i < orderedModuleIds.Count; i++)
            byId[orderedModuleIds[i]].SortOrder = i;

        _uow.CourseModules.UpdateRange(modules);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<int>> SaveLessonAsync(
        int schoolId, SaveLessonRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<int>.Failure("Tên bài học không được để trống.", "TITLE_REQUIRED");

        var module = await _uow.CourseModules.FindOneAsync(
            m => m.Id == request.ModuleId && m.Course.SchoolId == schoolId,
            q => q.Include(m => m.Course), ct);
        if (module is null)
            return Result<int>.Failure("Không tìm thấy chương.", "MODULE_NOT_FOUND");

        // Validate theo loại nội dung
        switch (request.ContentType)
        {
            case CourseLessonType.Video when string.IsNullOrWhiteSpace(request.VideoUrl):
                return Result<int>.Failure("Bài học dạng Video cần có VideoUrl.", "VIDEO_URL_REQUIRED");

            case CourseLessonType.Quiz:
                if (request.QuizExamId is null)
                    return Result<int>.Failure("Bài học dạng Quiz cần liên kết một đề trắc nghiệm.", "QUIZ_REQUIRED");
                var quizOk = await _uow.QuizExams.AnyAsync(
                    q => q.Id == request.QuizExamId.Value && q.SchoolId == schoolId, ct);
                if (!quizOk)
                    return Result<int>.Failure("Đề trắc nghiệm không hợp lệ.", "QUIZ_INVALID");
                break;
        }

        var minWatch = Math.Clamp(request.MinWatchPercent, (byte)10, (byte)100);

        CourseLesson lesson;
        if (request.Id.HasValue)
        {
            var existing = await _uow.CourseLessons.FirstOrDefaultAsync(
                l => l.Id == request.Id.Value && l.ModuleId == request.ModuleId, ct);
            if (existing is null)
                return Result<int>.Failure("Không tìm thấy bài học.", "LESSON_NOT_FOUND");
            lesson = existing;
        }
        else
        {
            var maxSort = await _uow.CourseLessons.AsQueryable()
                .Where(l => l.ModuleId == request.ModuleId)
                .MaxAsync(l => (int?)l.SortOrder, ct) ?? -1;
            lesson = new CourseLesson { ModuleId = request.ModuleId, SortOrder = maxSort + 1 };
            await _uow.CourseLessons.AddAsync(lesson, ct);
        }

        lesson.Title            = request.Title.Trim();
        lesson.ContentType      = request.ContentType;
        lesson.ContentHtml      = request.ContentHtml;
        lesson.VideoUrl         = request.VideoUrl;
        lesson.VideoDurationSec = request.VideoDurationSec;
        lesson.QuizExamId       = request.ContentType == CourseLessonType.Quiz ? request.QuizExamId : null;
        lesson.MinWatchPercent  = minWatch;
        lesson.IsPreviewable    = request.IsPreviewable;
        lesson.IsPublished      = request.IsPublished;
        lesson.Objectives       = request.Objectives;
        lesson.CognitiveLevel   = request.CognitiveLevel;
        lesson.PeriodCount      = Math.Clamp(request.PeriodCount, (byte)1, (byte)20);

        if (request.Id.HasValue) _uow.CourseLessons.Update(lesson);
        await _uow.SaveChangesAsync(ct);

        // Số bài published thay đổi ⇒ % tiến độ của mọi enrollment thay đổi
        await RecalcCourseEnrollmentsAsync(module.CourseId, ct);
        return Result<int>.Success(lesson.Id);
    }

    public async Task<Result> DeleteLessonAsync(int schoolId, int lessonId, CancellationToken ct = default)
    {
        var lesson = await _uow.CourseLessons.FindOneAsync(
            l => l.Id == lessonId && l.Module.Course.SchoolId == schoolId,
            q => q.Include(l => l.Module).ThenInclude(m => m.Course), ct);
        if (lesson is null)
            return Result.Failure("Không tìm thấy bài học.", "LESSON_NOT_FOUND");

        var hasProgress = await _uow.LessonProgresses.AnyAsync(p => p.CourseLessonId == lessonId, ct);
        if (hasProgress)
            return Result.Failure("Đã có học sinh học bài này — hãy ẩn (unpublish) thay vì xóa.", "LESSON_HAS_PROGRESS");

        var courseId = lesson.Module.CourseId;
        _uow.CourseLessons.Remove(lesson);   // Materials xóa theo; LastLessonId tự SetNull
        await _uow.SaveChangesAsync(ct);

        await RecalcCourseEnrollmentsAsync(courseId, ct);
        return Result.Success();
    }

    public async Task<Result> ReorderLessonsAsync(
        int schoolId, int moduleId, List<int> orderedLessonIds, CancellationToken ct = default)
    {
        var lessons = await _uow.CourseLessons.FindAsync(
            l => l.ModuleId == moduleId && l.Module.Course.SchoolId == schoolId, ct: ct);

        if (lessons.Count != orderedLessonIds.Count ||
            lessons.Select(l => l.Id).Except(orderedLessonIds).Any())
            return Result.Failure("Danh sách bài học không khớp với chương.", "REORDER_MISMATCH");

        var byId = lessons.ToDictionary(l => l.Id);
        for (var i = 0; i < orderedLessonIds.Count; i++)
            byId[orderedLessonIds[i]].SortOrder = i;

        _uow.CourseLessons.UpdateRange(lessons);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ══ Teacher/Admin: gán lớp & ghi danh ═════════════════════════════════════

    public async Task<Result<IReadOnlyList<ClassAssignmentDto>>> GetClassAssignmentsAsync(
        int schoolId, int courseId, CancellationToken ct = default)
    {
        var list = await _uow.ClassCourseAssignments.AsQueryable()
            .Where(a => a.CourseId == courseId && a.SchoolId == schoolId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ClassAssignmentDto(
                a.Id, a.ClassId, a.Class.ClassName,
                a.AssignedBy.FullName, a.CreatedAt,
                a.StartDate, a.EndDate, a.IsActive,
                a.Enrollments.Count(e => e.Status != CourseEnrollmentStatus.Dropped),
                a.Enrollments.Where(e => e.Status != CourseEnrollmentStatus.Dropped)
                             .Average(e => (decimal?)e.ProgressPercent) ?? 0M))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ClassAssignmentDto>>.Success(list);
    }

    public async Task<Result<int>> AssignToClassAsync(
        int schoolId, int courseId, int classId, int assignedByUserId, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result<int>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");
        if (course.Status != CourseStatus.Published)
            return Result<int>.Failure("Chỉ gán được khóa học đã phát hành (Published).", "COURSE_NOT_PUBLISHED");

        var cls = await _uow.Classes.FirstOrDefaultAsync(
            c => c.Id == classId && c.SchoolId == schoolId, ct);
        if (cls is null)
            return Result<int>.Failure("Không tìm thấy lớp học.", "CLASS_NOT_FOUND");

        var assignment = await _uow.ClassCourseAssignments.FirstOrDefaultAsync(
            a => a.CourseId == courseId && a.ClassId == classId, ct);

        var enrolled = 0;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            if (assignment is null)
            {
                assignment = new ClassCourseAssignment
                {
                    SchoolId         = schoolId,
                    CourseId         = courseId,
                    ClassId          = classId,
                    AssignedByUserId = assignedByUserId,
                    IsActive         = true
                };
                await _uow.ClassCourseAssignments.AddAsync(assignment, ct);
            }
            else
            {
                assignment.IsActive = true;   // gán lại lớp đã từng gỡ
                _uow.ClassCourseAssignments.Update(assignment);
            }
            await _uow.SaveChangesAsync(ct);

            enrolled = await EnrollActiveClassStudentsAsync(assignment, ct);
        }, ct);

        _logger.LogInformation("Gán khóa {CourseId} cho lớp {ClassId}: +{Enrolled} học sinh", courseId, classId, enrolled);
        return Result<int>.Success(enrolled);
    }

    public async Task<Result<int>> SyncClassEnrollmentsAsync(
        int schoolId, int classCourseAssignmentId, CancellationToken ct = default)
    {
        var assignment = await _uow.ClassCourseAssignments.FirstOrDefaultAsync(
            a => a.Id == classCourseAssignmentId && a.SchoolId == schoolId, ct);
        if (assignment is null)
            return Result<int>.Failure("Không tìm thấy phân công khóa học.", "ASSIGNMENT_NOT_FOUND");
        if (!assignment.IsActive)
            return Result<int>.Failure("Phân công này đã bị vô hiệu hóa.", "ASSIGNMENT_INACTIVE");

        var added = await EnrollActiveClassStudentsAsync(assignment, ct);
        return Result<int>.Success(added);
    }

    public async Task<Result> DeactivateClassAssignmentAsync(
        int schoolId, int classCourseAssignmentId, CancellationToken ct = default)
    {
        var assignment = await _uow.ClassCourseAssignments.FirstOrDefaultAsync(
            a => a.Id == classCourseAssignmentId && a.SchoolId == schoolId, ct);
        if (assignment is null)
            return Result.Failure("Không tìm thấy phân công khóa học.", "ASSIGNMENT_NOT_FOUND");

        // Học sinh đã ghi danh vẫn giữ quyền truy cập; chỉ ngừng ghi danh mới.
        assignment.IsActive = false;
        _uow.ClassCourseAssignments.Update(assignment);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<int>> EnrollStudentAsync(
        int schoolId, int courseId, int studentId, CourseEnrollmentSource source, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result<int>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");
        if (course.Status != CourseStatus.Published)
            return Result<int>.Failure("Khóa học chưa phát hành.", "COURSE_NOT_PUBLISHED");

        var student = await _uow.Users.FindOneAsync(
            u => u.Id == studentId && u.SchoolId == schoolId && u.IsActive,
            q => q.Include(u => u.Role), ct);
        if (student is null || student.Role.RoleCode != "STUDENT")
            return Result<int>.Failure("Học sinh không hợp lệ.", "STUDENT_INVALID");

        var existing = await _uow.CourseEnrollments.FirstOrDefaultAsync(
            e => e.CourseId == courseId && e.StudentId == studentId, ct);
        if (existing is not null)
        {
            if (existing.Status == CourseEnrollmentStatus.Dropped)
            {
                existing.Status = CourseEnrollmentStatus.Active;
                _uow.CourseEnrollments.Update(existing);
                await _uow.SaveChangesAsync(ct);
                return Result<int>.Success(existing.Id);
            }
            return Result<int>.Failure("Học sinh đã ghi danh khóa học này.", "ALREADY_ENROLLED");
        }

        var enrollment = new CourseEnrollment
        {
            SchoolId  = schoolId,
            CourseId  = courseId,
            StudentId = studentId,
            Source    = source
        };
        await _uow.CourseEnrollments.AddAsync(enrollment, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<int>.Success(enrollment.Id);
    }

    public async Task<Result<CourseProgressReportDto>> GetProgressReportAsync(
        int schoolId, int courseId, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FirstOrDefaultAsync(
            c => c.Id == courseId && c.SchoolId == schoolId, ct);
        if (course is null)
            return Result<CourseProgressReportDto>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        var totalLessons = await _uow.CourseLessons.CountAsync(
            l => l.Module.CourseId == courseId && l.IsPublished, ct);

        var rows = await _uow.CourseEnrollments.AsQueryable()
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Student.FullName)
            .Select(e => new StudentProgressRowDto(
                e.StudentId,
                e.Student.FullName,
                e.ClassAssignment != null ? e.ClassAssignment.Class.ClassName : null,
                e.ProgressPercent,
                e.CompletedLessonCount,
                e.Status,
                e.StartedAt,
                e.LastAccessedAt,
                e.CompletedAt))
            .ToListAsync(ct);

        var stats       = ComputeStats(rows.Where(r => r.Status != CourseEnrollmentStatus.Dropped)
                                           .Select(r => (r.ProgressPercent, r.Status)).ToList());
        var lessonStats = await ComputeLessonStatsAsync(courseId, stats.TotalActiveStudents, ct);

        var dto = new CourseProgressReportDto(
            courseId, course.Title, totalLessons,
            stats.TotalActiveStudents,
            stats.CompletedStudents,
            stats, rows, lessonStats);

        return Result<CourseProgressReportDto>.Success(dto);
    }

    public async Task<Result<CourseStatsDto>> GetCourseStatsAsync(
        int schoolId, int courseId, CancellationToken ct = default)
    {
        var enrollments = await _uow.CourseEnrollments.AsQueryable()
            .Where(e => e.CourseId == courseId && e.SchoolId == schoolId
                     && e.Status != CourseEnrollmentStatus.Dropped)
            .Select(e => new { e.ProgressPercent, e.Status })
            .ToListAsync(ct);

        return Result<CourseStatsDto>.Success(
            ComputeStats(enrollments.Select(e => (e.ProgressPercent, e.Status)).ToList()));
    }

    public async Task<Result<IReadOnlyList<ClassOptionDto>>> GetAssignableClassesAsync(
        int schoolId, CancellationToken ct = default)
    {
        var list = await _uow.Classes.AsQueryable()
            .Where(c => c.SchoolId == schoolId && c.IsActive)
            .OrderByDescending(c => c.AcademicYear.IsActive)
            .ThenBy(c => c.ClassName)
            .Select(c => new ClassOptionDto(c.Id, c.ClassName, c.AcademicYear.YearName, c.AcademicYear.IsActive))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ClassOptionDto>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<QuizExamOptionDto>>> GetLinkableQuizExamsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var list = await _uow.QuizExams.AsQueryable()
            .Where(q => q.SchoolId == schoolId && q.Status != QuizExamStatus.Draft)
            .OrderByDescending(q => q.CreatedAt)
            .Take(200)
            .Select(q => new QuizExamOptionDto(q.Id, q.Title, q.Subject.SubjectName, q.Status))
            .ToListAsync(ct);

        return Result<IReadOnlyList<QuizExamOptionDto>>.Success(list);
    }

    // ══ Nhà trường (Admin): giám sát — chỉ đọc ════════════════════════════════

    public async Task<Result<IReadOnlyList<TeacherCourseGroupDto>>> GetTeacherCourseOverviewAsync(
        int schoolId, CancellationToken ct = default)
    {
        // 1) Khóa học + số liệu tổng của khóa
        var courses = await _uow.Courses.AsQueryable()
            .Where(c => c.SchoolId == schoolId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Status,
                SubjectName    = c.Subject    != null ? c.Subject.SubjectName : null,
                GradeLevelName = c.GradeLevel != null ? c.GradeLevel.GradeName : null,
                TeacherId      = c.CreatedByUserId,
                TeacherName    = c.CreatedBy.FullName,
                TeacherRole    = c.CreatedBy.Role.RoleCode,
                LessonCount    = c.Modules.SelectMany(m => m.Lessons).Count(),
                QuizCount      = c.Modules.SelectMany(m => m.Lessons)
                                          .Count(l => l.ContentType == CourseLessonType.Quiz),
                Enrolled       = c.Enrollments.Count(e => e.Status != CourseEnrollmentStatus.Dropped),
                AvgPercent     = c.Enrollments.Where(e => e.Status != CourseEnrollmentStatus.Dropped)
                                              .Average(e => (decimal?)e.ProgressPercent)
            })
            .ToListAsync(ct);

        // 2) Lớp được gán khóa (query phẳng — gộp theo khóa ở bộ nhớ)
        var classRows = await _uow.ClassCourseAssignments.AsQueryable()
            .Where(a => a.SchoolId == schoolId)
            .Select(a => new
            {
                a.CourseId,
                a.ClassId,
                a.Class.ClassName,
                a.IsActive,
                StudentCount   = a.Enrollments.Count(e => e.Status != CourseEnrollmentStatus.Dropped),
                CompletedCount = a.Enrollments.Count(e => e.Status == CourseEnrollmentStatus.Completed),
                AvgPercent     = a.Enrollments.Where(e => e.Status != CourseEnrollmentStatus.Dropped)
                                              .Average(e => (decimal?)e.ProgressPercent)
            })
            .ToListAsync(ct);

        var classesByCourse = classRows
            .GroupBy(r => r.CourseId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(r => r.ClassName)
                .Select(r => new ManagedClassRowDto(
                    r.ClassId, r.ClassName, r.IsActive,
                    r.StudentCount, r.CompletedCount,
                    Math.Round(r.AvgPercent ?? 0M, 2)))
                .ToList());

        // 3) Mọi giáo viên đang hoạt động — kể cả người chưa có khóa nào
        var teachers = await _uow.Users.AsQueryable()
            .Where(u => u.SchoolId == schoolId && u.IsActive && u.Role.RoleCode == "TEACHER")
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        var groups = teachers.ToDictionary(
            t => t.Id,
            t => (Name: t.FullName, Role: "TEACHER", Courses: new List<ManagedCourseDto>()));

        foreach (var c in courses.OrderBy(c => c.Title))
        {
            if (!groups.TryGetValue(c.TeacherId, out var g))
            {
                // Khóa do nhà trường / vai trò khác tạo → vẫn hiển thị thành nhóm riêng
                g = (c.TeacherName, c.TeacherRole, new List<ManagedCourseDto>());
                groups[c.TeacherId] = g;
            }

            classesByCourse.TryGetValue(c.Id, out var classes);
            g.Courses.Add(new ManagedCourseDto(
                c.Id, c.Title, c.SubjectName, c.GradeLevelName, c.Status,
                c.LessonCount, c.QuizCount, c.Enrolled,
                Math.Round(c.AvgPercent ?? 0M, 2),
                classes ?? new List<ManagedClassRowDto>()));
        }

        var list = groups
            .Select(kv =>
            {
                var (name, roleCode, courseList) = kv.Value;
                var enrolled = courseList.Sum(c => c.EnrollmentCount);
                var avg = enrolled == 0
                    ? 0M
                    : Math.Round(courseList.Sum(c => c.AvgProgressPercent * c.EnrollmentCount) / enrolled, 2);

                return new TeacherCourseGroupDto(
                    kv.Key, name, RoleLabel(roleCode),
                    courseList.Count,
                    courseList.SelectMany(c => c.Classes).Select(x => x.ClassId).Distinct().Count(),
                    enrolled, avg, courseList);
            })
            .OrderByDescending(g => g.CourseCount)
            .ThenBy(g => g.TeacherName)
            .ToList();

        return Result<IReadOnlyList<TeacherCourseGroupDto>>.Success(list);
    }

    public async Task<Result<ClassCourseProgressDto>> GetClassCourseProgressAsync(
        int schoolId, int courseId, int classId, CancellationToken ct = default)
    {
        var course = await _uow.Courses.FindOneAsync(
            c => c.Id == courseId && c.SchoolId == schoolId,
            q => q.Include(c => c.Subject).Include(c => c.CreatedBy), ct);
        if (course is null)
            return Result<ClassCourseProgressDto>.Failure("Không tìm thấy khóa học.", "COURSE_NOT_FOUND");

        var cls = await _uow.Classes.FirstOrDefaultAsync(
            c => c.Id == classId && c.SchoolId == schoolId, ct);
        if (cls is null)
            return Result<ClassCourseProgressDto>.Failure("Không tìm thấy lớp học.", "CLASS_NOT_FOUND");

        var className = cls.ClassName;

        var totalLessons = await _uow.CourseLessons.CountAsync(
            l => l.Module.CourseId == courseId && l.IsPublished, ct);

        var classSize = await _uow.ClassEnrollments.CountAsync(
            e => e.ClassId == classId && e.Status == EnrollmentStatus.Active, ct);

        // ── Học sinh của lớp này trong khóa (ghi danh qua gán lớp) ────────────
        var students = await _uow.CourseEnrollments.AsQueryable()
            .Where(e => e.CourseId == courseId && e.SchoolId == schoolId
                     && e.ClassAssignment != null && e.ClassAssignment.ClassId == classId)
            .OrderBy(e => e.Student.FullName)
            .Select(e => new StudentProgressRowDto(
                e.StudentId,
                e.Student.FullName,
                className,
                e.ProgressPercent,
                e.CompletedLessonCount,
                e.Status,
                e.StartedAt,
                e.LastAccessedAt,
                e.CompletedAt))
            .ToListAsync(ct);

        var stats = ComputeStats(students.Where(s => s.Status != CourseEnrollmentStatus.Dropped)
                                         .Select(s => (s.ProgressPercent, s.Status)).ToList());

        // ── Tỷ lệ hoàn thành từng bài học — chỉ tính học sinh của lớp này ─────
        var agg = (await _uow.LessonProgresses.AsQueryable()
                .Where(p => p.Enrollment.CourseId == courseId
                         && p.Enrollment.Status != CourseEnrollmentStatus.Dropped
                         && p.Enrollment.ClassAssignment != null
                         && p.Enrollment.ClassAssignment.ClassId == classId
                         && p.Lesson.IsPublished)
                .GroupBy(p => p.CourseLessonId)
                .Select(g => new
                {
                    LessonId   = g.Key,
                    Completed  = g.Count(x => x.Status == ProgressStatus.Completed),
                    InProgress = g.Count(x => x.Status == ProgressStatus.InProgress)
                })
                .ToListAsync(ct))
            .ToDictionary(x => x.LessonId);

        var orderedLessons = await GetOrderedPublishedLessonsAsync(courseId, ct);
        var lessonRows = orderedLessons.Select(l =>
        {
            agg.TryGetValue(l.Id, out var a);
            var completed  = a?.Completed  ?? 0;
            var inProgress = a?.InProgress ?? 0;
            var rate = stats.TotalActiveStudents == 0
                ? 0M
                : Math.Round(completed * 100M / stats.TotalActiveStudents, 2);
            return new LessonStatRowDto(
                l.Id, l.Module.Title, l.Title, l.ContentType, completed, inProgress, rate);
        }).ToList();

        // ── Bài tập trên lớp (cùng môn với khóa nếu khóa gắn môn) ─────────────
        var homeworkRaw = await _uow.Assignments.AsQueryable()
            .Where(a => a.SchoolId == schoolId
                     && a.SubjectAssignment.ClassId == classId
                     && (course.SubjectId == null || a.SubjectAssignment.SubjectId == course.SubjectId))
            .OrderByDescending(a => a.DueDate)
            .ThenByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.AssignmentType,
                a.DueDate,
                a.IsPublished,
                a.MaxScore,
                TeacherName = a.SubjectAssignment.Teacher.FullName,
                Submitted   = a.Submissions.Count(s => s.SubmissionStatus != SubmissionStatus.Draft),
                Graded      = a.Submissions.Count(s => s.GradedAt != null),
                AvgScore    = a.Submissions.Where(s => s.Score != null).Average(s => s.Score)
            })
            .ToListAsync(ct);

        var homework = homeworkRaw.Select(a => new ClassHomeworkRowDto(
            a.Id, a.Title, a.AssignmentType, a.TeacherName, a.DueDate, a.IsPublished,
            a.Submitted, a.Graded, classSize,
            classSize == 0 ? 0M : Math.Round(a.Submitted * 100M / classSize, 2),
            a.AvgScore is null ? null : Math.Round(a.AvgScore.Value, 2),
            a.MaxScore)).ToList();

        var dto = new ClassCourseProgressDto(
            course.Id, course.Title, classId, className,
            course.CreatedBy?.FullName ?? "—",
            course.Subject?.SubjectName,
            course.Status,
            totalLessons, classSize,
            stats, lessonRows, homework, students);

        return Result<ClassCourseProgressDto>.Success(dto);
    }

    private static string RoleLabel(string roleCode) => roleCode switch
    {
        "TEACHER"    => "Giáo viên",
        "ADMIN"      => "Nhà trường",
        "SYSADMIN"   => "Quản trị hệ thống",
        "SUPERVISOR" => "Giám thị",
        _            => "Khác"
    };

    // ══ Student: học tập ══════════════════════════════════════════════════════

    public async Task<Result<IReadOnlyList<MyCourseDto>>> GetMyCoursesAsync(
        int schoolId, int studentId, CancellationToken ct = default)
    {
        var enrollments = await _uow.CourseEnrollments.AsQueryable()
            .Where(e => e.SchoolId == schoolId && e.StudentId == studentId
                     && e.Status != CourseEnrollmentStatus.Dropped
                     && e.Course.Status == CourseStatus.Published)
            .Select(e => new
            {
                e.Id, e.CourseId, e.Course.Title, e.Course.ThumbnailUrl,
                SubjectName = e.Course.Subject != null ? e.Course.Subject.SubjectName : null,
                e.ProgressPercent, e.CompletedLessonCount, e.LastLessonId, e.Status, e.LastAccessedAt,
                TotalLessons = e.Course.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished)
            })
            .OrderByDescending(e => e.LastAccessedAt)
            .ToListAsync(ct);

        var list = enrollments.Select(e => new MyCourseDto(
            e.CourseId, e.Id, e.Title, e.ThumbnailUrl, e.SubjectName,
            e.ProgressPercent, e.CompletedLessonCount, e.TotalLessons,
            e.LastLessonId, e.Status, e.LastAccessedAt)).ToList();

        return Result<IReadOnlyList<MyCourseDto>>.Success(list);
    }

    public async Task<Result<StudentCourseOutlineDto>> GetCourseOutlineAsync(
        int schoolId, int courseId, int studentId, CancellationToken ct = default)
    {
        var enrollment = await _uow.CourseEnrollments.FindOneAsync(
            e => e.CourseId == courseId && e.StudentId == studentId && e.SchoolId == schoolId,
            q => q.Include(e => e.ClassAssignment), ct);
        if (enrollment is null || enrollment.Status == CourseEnrollmentStatus.Dropped)
            return Result<StudentCourseOutlineDto>.Failure("Bạn chưa được ghi danh vào khóa học này.", "NOT_ENROLLED");

        var course = await _uow.Courses.FindOneAsync(
            c => c.Id == courseId && c.SchoolId == schoolId,
            q => q.Include(c => c.Modules).ThenInclude(m => m.Lessons), ct);
        if (course is null || course.Status != CourseStatus.Published)
            return Result<StudentCourseOutlineDto>.Failure("Khóa học không khả dụng.", "COURSE_UNAVAILABLE");

        var accessError = CheckAccessWindow(enrollment);
        if (accessError is not null)
            return Result<StudentCourseOutlineDto>.Failure(accessError, "ACCESS_EXPIRED");

        var progressByLesson = (await _uow.LessonProgresses.FindAsync(
                p => p.EnrollmentId == enrollment.Id, ct: ct))
            .ToDictionary(p => p.CourseLessonId);

        var now = DateTime.UtcNow;
        var sequentialGate = false;   // true kể từ sau bài chưa hoàn thành đầu tiên
        var modules = new List<StudentModuleDto>();
        int? resumeLessonId = null;

        foreach (var module in course.Modules.OrderBy(m => m.SortOrder).ThenBy(m => m.Id))
        {
            var (moduleLocked, lockReason) = CheckModuleLock(module, enrollment, now);
            var lessonRows = new List<StudentLessonRowDto>();

            foreach (var lesson in module.Lessons.Where(l => l.IsPublished)
                                                 .OrderBy(l => l.SortOrder).ThenBy(l => l.Id))
            {
                progressByLesson.TryGetValue(lesson.Id, out var progress);
                var isCompleted = progress?.Status == ProgressStatus.Completed;

                var locked = moduleLocked || (course.IsSequential && sequentialGate && !isCompleted);
                lessonRows.Add(new StudentLessonRowDto(
                    lesson.Id, lesson.Title, lesson.SortOrder, lesson.ContentType,
                    lesson.VideoDurationSec, locked, progress?.Status));

                if (!isCompleted)
                {
                    if (!locked && resumeLessonId is null)
                        resumeLessonId = lesson.Id;
                    sequentialGate = true;
                }
            }

            if (lessonRows.Count > 0)
                modules.Add(new StudentModuleDto(
                    module.Id, module.Title, module.Description, module.SortOrder,
                    moduleLocked, lockReason, lessonRows));
        }

        var totalLessons = modules.Sum(m => m.Lessons.Count);
        var dto = new StudentCourseOutlineDto(
            course.Id, enrollment.Id, course.Title, course.Description, course.IsSequential,
            enrollment.ProgressPercent, enrollment.CompletedLessonCount, totalLessons,
            enrollment.LastLessonId ?? resumeLessonId, modules);

        return Result<StudentCourseOutlineDto>.Success(dto);
    }

    public async Task<Result<StudentLessonContentDto>> OpenLessonAsync(
        int schoolId, int lessonId, int studentId, CancellationToken ct = default)
    {
        var lesson = await _uow.CourseLessons.FindOneAsync(
            l => l.Id == lessonId && l.Module.Course.SchoolId == schoolId,
            q => q.Include(l => l.Module).ThenInclude(m => m.Course)
                  .Include(l => l.Materials), ct);
        if (lesson is null || !lesson.IsPublished)
            return Result<StudentLessonContentDto>.Failure("Không tìm thấy bài học.", "LESSON_NOT_FOUND");

        var course = lesson.Module.Course;
        if (course.Status != CourseStatus.Published)
            return Result<StudentLessonContentDto>.Failure("Khóa học không khả dụng.", "COURSE_UNAVAILABLE");

        var enrollment = await _uow.CourseEnrollments.FindOneAsync(
            e => e.CourseId == course.Id && e.StudentId == studentId,
            q => q.Include(e => e.ClassAssignment), ct);

        // Chưa ghi danh: chỉ được xem bài preview
        if (enrollment is null || enrollment.Status == CourseEnrollmentStatus.Dropped)
        {
            if (!lesson.IsPreviewable)
                return Result<StudentLessonContentDto>.Failure("Bạn chưa được ghi danh vào khóa học này.", "NOT_ENROLLED");
            return Result<StudentLessonContentDto>.Success(BuildLessonContent(lesson, null, null, null));
        }

        var accessError = CheckAccessWindow(enrollment);
        if (accessError is not null)
            return Result<StudentLessonContentDto>.Failure(accessError, "ACCESS_EXPIRED");

        // Kiểm tra khóa drip / sequential trên toàn cấu trúc khóa học
        var orderedLessons = await GetOrderedPublishedLessonsAsync(course.Id, ct);
        var progressList = await _uow.LessonProgresses.FindAsync(
            p => p.EnrollmentId == enrollment.Id, ct: ct);
        var progressByLesson = progressList.ToDictionary(p => p.CourseLessonId);

        var (moduleLocked, moduleReason) = CheckModuleLock(lesson.Module, enrollment, DateTime.UtcNow);
        if (moduleLocked)
            return Result<StudentLessonContentDto>.Failure(moduleReason ?? "Chương này chưa mở.", "MODULE_LOCKED");

        if (course.IsSequential)
        {
            foreach (var l in orderedLessons)
            {
                if (l.Id == lessonId) break;
                if (progressByLesson.TryGetValue(l.Id, out var pr) && pr.Status == ProgressStatus.Completed) continue;
                return Result<StudentLessonContentDto>.Failure(
                    $"Bạn cần hoàn thành bài \"{l.Title}\" trước.", "LESSON_LOCKED");
            }
        }

        // Lazy-create LessonProgress (unique index chống double-create khi race)
        progressByLesson.TryGetValue(lessonId, out var progress);
        if (progress is null)
        {
            progress = new LessonProgress
            {
                SchoolId       = schoolId,
                EnrollmentId   = enrollment.Id,
                CourseLessonId = lessonId
            };
            await _uow.LessonProgresses.AddAsync(progress, ct);
        }

        enrollment.StartedAt      ??= DateTime.UtcNow;
        enrollment.LastAccessedAt   = DateTime.UtcNow;
        enrollment.LastLessonId     = lessonId;
        _uow.CourseEnrollments.Update(enrollment);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race trên unique (EnrollmentId, CourseLessonId) — lấy row đã tồn tại
            progress = await _uow.LessonProgresses.FirstOrDefaultAsync(
                p => p.EnrollmentId == enrollment.Id && p.CourseLessonId == lessonId, ct)
                ?? throw new InvalidOperationException("Không tạo được tiến độ bài học.");
        }

        var idx    = orderedLessons.FindIndex(l => l.Id == lessonId);
        int? prevId = idx > 0 ? orderedLessons[idx - 1].Id : null;
        int? nextId = idx >= 0 && idx < orderedLessons.Count - 1 ? orderedLessons[idx + 1].Id : null;

        return Result<StudentLessonContentDto>.Success(BuildLessonContent(lesson, progress, prevId, nextId));
    }

    public async Task<Result<VideoHeartbeatResponse>> UpdateVideoProgressAsync(
        int schoolId, int lessonId, int studentId, VideoHeartbeatRequest request, CancellationToken ct = default)
    {
        var (progress, lesson, enrollment, error) =
            await GetProgressContextAsync(schoolId, lessonId, studentId, ct);
        if (error is not null)
            return Result<VideoHeartbeatResponse>.Failure(error.Value.msg, error.Value.code);

        if (lesson!.ContentType != CourseLessonType.Video)
            return Result<VideoHeartbeatResponse>.Failure("Bài học này không phải dạng Video.", "NOT_VIDEO");

        // ── Clamp chống gian lận: delta không thể lớn hơn thời gian thực trôi qua ──
        var elapsedSec = (DateTime.UtcNow - progress!.UpdatedAt).TotalSeconds;
        var maxDelta   = (int)Math.Min(elapsedSec * 1.5 + 10, MaxHeartbeatDeltaSec);
        var delta      = Math.Clamp(request.WatchedDeltaSec, 0, maxDelta);

        progress.TimeSpentSec += delta;
        progress.WatchedSec = lesson.VideoDurationSec.HasValue
            ? Math.Min(progress.WatchedSec + delta, lesson.VideoDurationSec.Value)
            : progress.WatchedSec + delta;
        progress.LastPositionSec = lesson.VideoDurationSec.HasValue
            ? Math.Clamp(request.PositionSec, 0, lesson.VideoDurationSec.Value)
            : Math.Max(request.PositionSec, 0);

        var completedNow = false;
        if (progress.Status != ProgressStatus.Completed &&
            lesson.VideoDurationSec is > 0 &&
            progress.WatchedSec >= lesson.VideoDurationSec.Value * lesson.MinWatchPercent / 100.0)
        {
            MarkCompleted(progress);
            completedNow = true;
        }

        _uow.LessonProgresses.Update(progress);
        enrollment!.LastAccessedAt = DateTime.UtcNow;
        _uow.CourseEnrollments.Update(enrollment);
        await _uow.SaveChangesAsync(ct);

        if (completedNow)
            await RecalcEnrollmentAsync(enrollment, ct);

        return Result<VideoHeartbeatResponse>.Success(new VideoHeartbeatResponse(
            progress.WatchedSec,
            progress.Status == ProgressStatus.Completed,
            enrollment.ProgressPercent));
    }

    public async Task<Result<LessonCompletionResultDto>> CompleteLessonAsync(
        int schoolId, int lessonId, int studentId, CancellationToken ct = default)
    {
        var (progress, lesson, enrollment, error) =
            await GetProgressContextAsync(schoolId, lessonId, studentId, ct);
        if (error is not null)
            return Result<LessonCompletionResultDto>.Failure(error.Value.msg, error.Value.code);

        if (progress!.Status == ProgressStatus.Completed)
            return Result<LessonCompletionResultDto>.Success(new LessonCompletionResultDto(
                true, enrollment!.ProgressPercent, enrollment.Status == CourseEnrollmentStatus.Completed));

        switch (lesson!.ContentType)
        {
            case CourseLessonType.Article:
                break;   // đọc xong là hoàn thành

            case CourseLessonType.Video:
                // Chỉ chặn khi biết thời lượng; video không rõ duration cho complete tay
                if (lesson.VideoDurationSec is > 0 &&
                    progress.WatchedSec < lesson.VideoDurationSec.Value * lesson.MinWatchPercent / 100.0)
                    return Result<LessonCompletionResultDto>.Failure(
                        $"Bạn cần xem tối thiểu {lesson.MinWatchPercent}% thời lượng video.", "WATCH_MORE");
                break;

            case CourseLessonType.Quiz:
                var attempt = await _uow.StudentQuizAttempts.FirstOrDefaultAsync(
                    a => a.ExamId == lesson.QuizExamId && a.StudentId == studentId && a.SubmittedAt != null, ct);
                if (attempt is null)
                    return Result<LessonCompletionResultDto>.Failure(
                        "Bạn cần nộp bài trắc nghiệm trước khi hoàn thành bài học.", "QUIZ_NOT_SUBMITTED");
                progress.QuizAttemptId = attempt.Id;
                break;
        }

        MarkCompleted(progress);
        _uow.LessonProgresses.Update(progress);
        await _uow.SaveChangesAsync(ct);

        await RecalcEnrollmentAsync(enrollment!, ct);

        return Result<LessonCompletionResultDto>.Success(new LessonCompletionResultDto(
            true, enrollment!.ProgressPercent, enrollment.Status == CourseEnrollmentStatus.Completed));
    }

    // ══ Private helpers ═══════════════════════════════════════════════════════

    /// <summary>Ghi danh mọi học sinh Active của lớp chưa có enrollment. Trả về số lượng thêm mới.</summary>
    private async Task<int> EnrollActiveClassStudentsAsync(ClassCourseAssignment assignment, CancellationToken ct)
    {
        var studentIds = await _uow.ClassEnrollments.AsQueryable()
            .Where(e => e.ClassId == assignment.ClassId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        var alreadyEnrolled = await _uow.CourseEnrollments.AsQueryable()
            .Where(e => e.CourseId == assignment.CourseId && studentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        var newIds = studentIds.Except(alreadyEnrolled).ToList();
        if (newIds.Count == 0) return 0;

        var enrollments = newIds.Select(sid => new CourseEnrollment
        {
            SchoolId                = assignment.SchoolId,
            CourseId                = assignment.CourseId,
            StudentId               = sid,
            Source                  = CourseEnrollmentSource.ClassAssigned,
            ClassCourseAssignmentId = assignment.Id
        });

        await _uow.CourseEnrollments.AddRangeAsync(enrollments, ct);
        await _uow.SaveChangesAsync(ct);
        return newIds.Count;
    }

    /// <summary>null = còn hạn truy cập; ngược lại trả về thông báo lỗi.</summary>
    private static string? CheckAccessWindow(CourseEnrollment enrollment)
    {
        var a = enrollment.ClassAssignment;
        if (a is null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (a.StartDate.HasValue && today < a.StartDate.Value)
            return $"Khóa học mở từ ngày {a.StartDate:dd/MM/yyyy}.";
        if (a.EndDate.HasValue && today > a.EndDate.Value)
            return "Thời gian truy cập khóa học của lớp đã kết thúc.";
        return null;
    }

    private static (bool locked, string? reason) CheckModuleLock(
        CourseModule module, CourseEnrollment enrollment, DateTime nowUtc)
    {
        if (module.AvailableFrom.HasValue && nowUtc < module.AvailableFrom.Value)
            return (true, $"Chương mở từ {module.AvailableFrom:dd/MM/yyyy HH:mm}.");

        if (module.UnlockAfterDays is > 0)
        {
            var unlockAt = enrollment.CreatedAt.AddDays(module.UnlockAfterDays.Value);
            if (nowUtc < unlockAt)
                return (true, $"Chương mở sau {module.UnlockAfterDays} ngày kể từ khi ghi danh ({unlockAt:dd/MM/yyyy}).");
        }
        return (false, null);
    }

    /// <summary>Bài học published của khóa theo thứ tự toàn cục (module.SortOrder → lesson.SortOrder).</summary>
    private async Task<List<CourseLesson>> GetOrderedPublishedLessonsAsync(int courseId, CancellationToken ct)
    {
        var lessons = await _uow.CourseLessons.FindAsync(
            l => l.Module.CourseId == courseId && l.IsPublished,
            q => q.Include(l => l.Module), ct);

        return lessons
            .OrderBy(l => l.Module.SortOrder).ThenBy(l => l.Module.Id)
            .ThenBy(l => l.SortOrder).ThenBy(l => l.Id)
            .ToList();
    }

    private static void MarkCompleted(LessonProgress progress)
    {
        progress.Status      = ProgressStatus.Completed;
        progress.CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Gom thống kê hoàn thành: trung bình + phân bố 4 khoảng (không tính Dropped).</summary>
    private static CourseStatsDto ComputeStats(
        IReadOnlyList<(decimal ProgressPercent, CourseEnrollmentStatus Status)> enrollments)
    {
        if (enrollments.Count == 0)
            return new CourseStatsDto(0M, 0, 0, 0, 0, 0, 0);

        var avg = Math.Round(enrollments.Average(e => e.ProgressPercent), 2);
        return new CourseStatsDto(
            avg,
            enrollments.Count,
            enrollments.Count(e => e.Status == CourseEnrollmentStatus.Completed),
            enrollments.Count(e => e.ProgressPercent < 25M),
            enrollments.Count(e => e.ProgressPercent is >= 25M and < 50M),
            enrollments.Count(e => e.ProgressPercent is >= 50M and < 75M),
            enrollments.Count(e => e.ProgressPercent >= 75M));
    }

    /// <summary>Tỷ lệ hoàn thành theo từng bài học published, theo thứ tự giáo trình.</summary>
    private async Task<List<LessonStatRowDto>> ComputeLessonStatsAsync(
        int courseId, int totalActiveStudents, CancellationToken ct)
    {
        var agg = (await _uow.LessonProgresses.AsQueryable()
                .Where(p => p.Enrollment.CourseId == courseId
                         && p.Enrollment.Status != CourseEnrollmentStatus.Dropped
                         && p.Lesson.IsPublished)
                .GroupBy(p => p.CourseLessonId)
                .Select(g => new
                {
                    LessonId   = g.Key,
                    Completed  = g.Count(x => x.Status == ProgressStatus.Completed),
                    InProgress = g.Count(x => x.Status == ProgressStatus.InProgress)
                })
                .ToListAsync(ct))
            .ToDictionary(x => x.LessonId);

        var lessons = await GetOrderedPublishedLessonsAsync(courseId, ct);

        return lessons.Select(l =>
        {
            agg.TryGetValue(l.Id, out var a);
            var completed  = a?.Completed ?? 0;
            var inProgress = a?.InProgress ?? 0;
            var rate = totalActiveStudents == 0
                ? 0M
                : Math.Round(completed * 100M / totalActiveStudents, 2);
            return new LessonStatRowDto(
                l.Id, l.Module.Title, l.Title, l.ContentType, completed, inProgress, rate);
        }).ToList();
    }

    private static StudentLessonContentDto BuildLessonContent(
        CourseLesson lesson, LessonProgress? progress, int? prevId, int? nextId) =>
        new(
            lesson.Id, lesson.ModuleId, lesson.Title, lesson.ContentType,
            lesson.ContentHtml, lesson.VideoUrl, lesson.VideoDurationSec,
            lesson.QuizExamId, lesson.MinWatchPercent, lesson.Objectives,
            progress?.LastPositionSec ?? 0,
            progress?.WatchedSec ?? 0,
            progress?.Status == ProgressStatus.Completed,
            prevId, nextId,
            lesson.Materials.OrderBy(m => m.SortOrder)
                .Select(m => new CourseMaterialDto(m.Id, m.FileName, m.FileUrl, m.FileType, m.FileSizeKB))
                .ToList());

    /// <summary>Ngữ cảnh chung cho heartbeat/complete: lesson + enrollment + progress (bắt buộc đã Open).</summary>
    private async Task<(LessonProgress? progress, CourseLesson? lesson, CourseEnrollment? enrollment,
        (string msg, string code)? error)> GetProgressContextAsync(
        int schoolId, int lessonId, int studentId, CancellationToken ct)
    {
        var lesson = await _uow.CourseLessons.FindOneAsync(
            l => l.Id == lessonId && l.Module.Course.SchoolId == schoolId,
            q => q.Include(l => l.Module).ThenInclude(m => m.Course), ct);
        if (lesson is null || !lesson.IsPublished)
            return (null, null, null, ("Không tìm thấy bài học.", "LESSON_NOT_FOUND"));

        var enrollment = await _uow.CourseEnrollments.FindOneAsync(
            e => e.CourseId == lesson.Module.CourseId && e.StudentId == studentId,
            q => q.Include(e => e.ClassAssignment), ct);
        if (enrollment is null || enrollment.Status == CourseEnrollmentStatus.Dropped)
            return (null, lesson, null, ("Bạn chưa được ghi danh vào khóa học này.", "NOT_ENROLLED"));

        var accessError = CheckAccessWindow(enrollment);
        if (accessError is not null)
            return (null, lesson, enrollment, (accessError, "ACCESS_EXPIRED"));

        var progress = await _uow.LessonProgresses.FirstOrDefaultAsync(
            p => p.EnrollmentId == enrollment.Id && p.CourseLessonId == lessonId, ct);
        if (progress is null)
            return (null, lesson, enrollment, ("Hãy mở bài học trước khi cập nhật tiến độ.", "PROGRESS_NOT_STARTED"));

        return (progress, lesson, enrollment, null);
    }

    /// <summary>Tính lại % tiến độ + trạng thái hoàn thành cho 1 enrollment.</summary>
    private async Task RecalcEnrollmentAsync(CourseEnrollment enrollment, CancellationToken ct)
    {
        var total = await _uow.CourseLessons.CountAsync(
            l => l.Module.CourseId == enrollment.CourseId && l.IsPublished, ct);

        var completed = await _uow.LessonProgresses.CountAsync(
            p => p.EnrollmentId == enrollment.Id
                 && p.Status == ProgressStatus.Completed
                 && p.Lesson.IsPublished, ct);

        ApplyProgress(enrollment, completed, total);
        _uow.CourseEnrollments.Update(enrollment);
        await _uow.SaveChangesAsync(ct);
    }

    /// <summary>Tính lại tiến độ cho MỌI enrollment của khóa (gọi khi cấu trúc bài học thay đổi).</summary>
    private async Task RecalcCourseEnrollmentsAsync(int courseId, CancellationToken ct)
    {
        var total = await _uow.CourseLessons.CountAsync(
            l => l.Module.CourseId == courseId && l.IsPublished, ct);

        var enrollments = await _uow.CourseEnrollments.FindAsync(
            e => e.CourseId == courseId && e.Status != CourseEnrollmentStatus.Dropped, ct: ct);
        if (enrollments.Count == 0) return;

        var completedByEnrollment = (await _uow.LessonProgresses.AsQueryable()
                .Where(p => p.Enrollment.CourseId == courseId
                         && p.Status == ProgressStatus.Completed
                         && p.Lesson.IsPublished)
                .GroupBy(p => p.EnrollmentId)
                .Select(g => new { EnrollmentId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.EnrollmentId, x => x.Count);

        foreach (var enrollment in enrollments)
        {
            completedByEnrollment.TryGetValue(enrollment.Id, out var completed);
            ApplyProgress(enrollment, completed, total);
        }

        _uow.CourseEnrollments.UpdateRange(enrollments);
        await _uow.SaveChangesAsync(ct);
    }

    private static void ApplyProgress(CourseEnrollment enrollment, int completed, int total)
    {
        enrollment.CompletedLessonCount = completed;
        enrollment.ProgressPercent      = total == 0 ? 0M : Math.Round(completed * 100M / total, 2);

        if (total > 0 && completed >= total)
        {
            if (enrollment.Status == CourseEnrollmentStatus.Active)
            {
                enrollment.Status       = CourseEnrollmentStatus.Completed;
                enrollment.CompletedAt ??= DateTime.UtcNow;
            }
        }
        else if (enrollment.Status == CourseEnrollmentStatus.Completed)
        {
            // Khóa học được thêm bài mới sau khi đã hoàn thành → quay lại Active
            enrollment.Status      = CourseEnrollmentStatus.Active;
            enrollment.CompletedAt = null;
        }
    }
}
