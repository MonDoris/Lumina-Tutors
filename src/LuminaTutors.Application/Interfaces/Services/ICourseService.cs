using LuminaTutors.Application.DTOs.Course;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// E-Learning: quản trị khóa học (Course → Module → Lesson), gán lớp / ghi danh
/// và theo dõi tiến độ học tập của học sinh.
/// </summary>
public interface ICourseService
{
    // ══ Teacher/Admin: Course CRUD ════════════════════════════════════════════

    Task<Result<IReadOnlyList<CourseListItemDto>>> GetCoursesAsync(
        int schoolId, int? createdByUserId = null, CancellationToken ct = default);

    Task<Result<CourseDetailDto>> GetCourseDetailAsync(
        int schoolId, int courseId, CancellationToken ct = default);

    Task<Result<int>> CreateCourseAsync(
        int schoolId, int userId, CreateCourseRequest request, CancellationToken ct = default);

    Task<Result> UpdateCourseAsync(
        int schoolId, int courseId, UpdateCourseRequest request, CancellationToken ct = default);

    /// <summary>Chỉ Publish được khi có ≥ 1 bài học đã publish. Archive để gỡ khỏi danh sách học.</summary>
    Task<Result> ChangeCourseStatusAsync(
        int schoolId, int courseId, CourseStatus status, CancellationToken ct = default);

    /// <summary>Chỉ xóa được khóa Draft chưa có ghi danh; ngược lại dùng Archive.</summary>
    Task<Result> DeleteCourseAsync(int schoolId, int courseId, CancellationToken ct = default);

    // ══ Teacher/Admin: Module & Lesson ════════════════════════════════════════

    Task<Result<int>> SaveModuleAsync(int schoolId, SaveModuleRequest request, CancellationToken ct = default);
    Task<Result> DeleteModuleAsync(int schoolId, int moduleId, CancellationToken ct = default);
    Task<Result> ReorderModulesAsync(int schoolId, int courseId, List<int> orderedModuleIds, CancellationToken ct = default);

    Task<Result<int>> SaveLessonAsync(int schoolId, SaveLessonRequest request, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(int schoolId, int lessonId, CancellationToken ct = default);
    Task<Result> ReorderLessonsAsync(int schoolId, int moduleId, List<int> orderedLessonIds, CancellationToken ct = default);

    // ══ Teacher/Admin: gán lớp & ghi danh ═════════════════════════════════════

    Task<Result<IReadOnlyList<ClassAssignmentDto>>> GetClassAssignmentsAsync(
        int schoolId, int courseId, CancellationToken ct = default);

    /// <summary>Gán khóa cho lớp + tự động ghi danh mọi học sinh đang Active của lớp.</summary>
    Task<Result<int>> AssignToClassAsync(
        int schoolId, int courseId, int classId, int assignedByUserId, CancellationToken ct = default);

    /// <summary>Ghi danh bổ sung học sinh vào lớp sau khi đã gán khóa (bấm "đồng bộ").</summary>
    Task<Result<int>> SyncClassEnrollmentsAsync(
        int schoolId, int classCourseAssignmentId, CancellationToken ct = default);

    Task<Result> DeactivateClassAssignmentAsync(
        int schoolId, int classCourseAssignmentId, CancellationToken ct = default);

    Task<Result<int>> EnrollStudentAsync(
        int schoolId, int courseId, int studentId, CourseEnrollmentSource source, CancellationToken ct = default);

    Task<Result<CourseProgressReportDto>> GetProgressReportAsync(
        int schoolId, int courseId, CancellationToken ct = default);

    /// <summary>Thống kê hoàn thành của khóa: % trung bình + phân bố 4 khoảng (panel builder).</summary>
    Task<Result<CourseStatsDto>> GetCourseStatsAsync(
        int schoolId, int courseId, CancellationToken ct = default);

    /// <summary>Danh sách lớp đang hoạt động (dropdown gán khóa).</summary>
    Task<Result<IReadOnlyList<ClassOptionDto>>> GetAssignableClassesAsync(
        int schoolId, CancellationToken ct = default);

    /// <summary>Đề trắc nghiệm có thể liên kết vào bài học dạng Quiz (đã publish/closed).</summary>
    Task<Result<IReadOnlyList<QuizExamOptionDto>>> GetLinkableQuizExamsAsync(
        int schoolId, CancellationToken ct = default);

    // ══ Nhà trường (Admin): giám sát — chỉ đọc ════════════════════════════════

    /// <summary>
    /// Toàn bộ khóa học của trường gom theo GIÁO VIÊN phụ trách, mỗi khóa kèm danh
    /// sách lớp được gán và % hoàn thành của từng lớp. Giáo viên chưa có khóa vẫn
    /// xuất hiện (CourseCount = 0) để nhà trường thấy ai chưa triển khai.
    /// </summary>
    Task<Result<IReadOnlyList<TeacherCourseGroupDto>>> GetTeacherCourseOverviewAsync(
        int schoolId, CancellationToken ct = default);

    /// <summary>
    /// Chi tiết 1 khóa học trên 1 lớp: tỷ lệ hoàn thành từng bài trong khóa,
    /// bài tập trên lớp (cùng môn) kèm tỷ lệ nộp, và % của từng học sinh.
    /// </summary>
    Task<Result<ClassCourseProgressDto>> GetClassCourseProgressAsync(
        int schoolId, int courseId, int classId, CancellationToken ct = default);

    // ══ Student: học tập ══════════════════════════════════════════════════════

    Task<Result<IReadOnlyList<MyCourseDto>>> GetMyCoursesAsync(
        int schoolId, int studentId, CancellationToken ct = default);

    /// <summary>Mục lục khóa học kèm trạng thái khóa/mở & tiến độ từng bài của học sinh.</summary>
    Task<Result<StudentCourseOutlineDto>> GetCourseOutlineAsync(
        int schoolId, int courseId, int studentId, CancellationToken ct = default);

    /// <summary>
    /// Mở bài học: kiểm tra quyền + drip/sequential lock, tạo LessonProgress (lazy),
    /// cập nhật resume point. Trả về nội dung bài.
    /// </summary>
    Task<Result<StudentLessonContentDto>> OpenLessonAsync(
        int schoolId, int lessonId, int studentId, CancellationToken ct = default);

    /// <summary>Heartbeat video: cộng dồn WatchedSec (có clamp chống tua), auto-complete khi đạt ngưỡng.</summary>
    Task<Result<VideoHeartbeatResponse>> UpdateVideoProgressAsync(
        int schoolId, int lessonId, int studentId, VideoHeartbeatRequest request, CancellationToken ct = default);

    /// <summary>
    /// Đánh dấu hoàn thành: Article → hoàn thành ngay; Quiz → kiểm tra có attempt đã nộp;
    /// Video → chỉ khi đã xem đủ ngưỡng MinWatchPercent.
    /// </summary>
    Task<Result<LessonCompletionResultDto>> CompleteLessonAsync(
        int schoolId, int lessonId, int studentId, CancellationToken ct = default);
}
