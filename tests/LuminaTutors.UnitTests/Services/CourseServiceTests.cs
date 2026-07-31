using LuminaTutors.Application.DTOs.Course;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="CourseService"/> — khóa học E-Learning: tạo/sửa/xóa khóa,
/// đổi trạng thái phát hành, quản lý chương (module) và bài học (lesson) với đầy đủ ràng buộc.
/// </summary>
public class CourseServiceTests : ServiceTestBase
{
    private CourseService CreateSut() => new(Uow.Object, NullLogger<CourseService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateCourse
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCourse_TenRong_TraVeTitleRequired()
    {
        var result = await CreateSut().CreateCourseAsync(1, 9,
            new CreateCourseRequest("  ", null, null, null, null, false));

        ShouldFail(result, "TITLE_REQUIRED");
    }

    [Fact]
    public async Task CreateCourse_MonHocKhongHopLe_TraVeSubjectInvalid()
    {
        Repo(s => s.Subjects).SetupAny(false);

        var result = await CreateSut().CreateCourseAsync(1, 9,
            new CreateCourseRequest("Khóa Toán 10", null, null, SubjectId: 5, GradeLevelId: null, false));

        ShouldFail(result, "SUBJECT_INVALID");
    }

    [Fact]
    public async Task CreateCourse_KhoiLopKhongHopLe_TraVeGradeLevelInvalid()
    {
        Repo(g => g.GradeLevels).SetupAny(false);

        var result = await CreateSut().CreateCourseAsync(1, 9,
            new CreateCourseRequest("Khóa Toán 10", null, null, SubjectId: null, GradeLevelId: 3, false));

        ShouldFail(result, "GRADELEVEL_INVALID");
    }

    [Fact]
    public async Task CreateCourse_HopLe_TaoKhoaVaTraVeId()
    {
        var added = Repo(c => c.Courses).CaptureAdds();

        var result = await CreateSut().CreateCourseAsync(1, 9,
            new CreateCourseRequest("  Khóa Toán 10  ", "Mô tả", null, null, null, IsSequential: true));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].Title.Should().Be("Khóa Toán 10");   // đã Trim()
        added[0].Status.Should().Be(CourseStatus.Draft);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Update / ChangeStatus / Delete
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCourse_KhongTonTai_TraVeCourseNotFound()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(null);

        var result = await CreateSut().UpdateCourseAsync(1, 5,
            new UpdateCourseRequest("Tên mới", null, null, null, null, false));

        ShouldFail(result, "COURSE_NOT_FOUND");
    }

    [Fact]
    public async Task ChangeStatus_PhatHanhKhiChuaCoBaiPublished_TraVeLoi()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1, Status = CourseStatus.Draft });
        Repo(l => l.CourseLessons).SetupAny(false); // chưa có bài học nào publish

        var result = await CreateSut().ChangeCourseStatusAsync(1, 5, CourseStatus.Published);

        ShouldFail(result, "NO_PUBLISHED_LESSON");
    }

    [Fact]
    public async Task ChangeStatus_PhatHanhHopLe_ThanhCong()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1, Status = CourseStatus.Draft });
        Repo(l => l.CourseLessons).SetupAny(true); // đã có bài publish

        var result = await CreateSut().ChangeCourseStatusAsync(1, 5, CourseStatus.Published);

        result.IsSuccess.Should().BeTrue();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task DeleteCourse_KhongPhaiNhap_TraVeNotDraft()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1, Status = CourseStatus.Published });

        var result = await CreateSut().DeleteCourseAsync(1, 5);

        ShouldFail(result, "COURSE_NOT_DRAFT");
    }

    [Fact]
    public async Task DeleteCourse_DaCoHocSinhGhiDanh_TraVeHasEnrollments()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1, Status = CourseStatus.Draft });
        Repo(e => e.CourseEnrollments).SetupAny(true);

        var result = await CreateSut().DeleteCourseAsync(1, 5);

        ShouldFail(result, "COURSE_HAS_ENROLLMENTS");
    }

    [Fact]
    public async Task DeleteCourse_HopLe_Xoa()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1, Status = CourseStatus.Draft });
        Repo(e => e.CourseEnrollments).SetupAny(false);
        Repo(a => a.ClassCourseAssignments).SetupAny(false);

        var result = await CreateSut().DeleteCourseAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        Repo(c => c.Courses).Verify(r => r.Remove(It.IsAny<Course>()), Times.Once());
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Module (chương)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveModule_KhoaHocKhongTonTai_TraVeCourseNotFound()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(null);

        var req = new SaveModuleRequest(null, 5, "Chương 1", null, null, null);
        var result = await CreateSut().SaveModuleAsync(1, req);

        ShouldFail(result, "COURSE_NOT_FOUND");
    }

    [Fact]
    public async Task SaveModule_HocKyKhongHopLe_TraVeSemesterInvalid()
    {
        Repo(c => c.Courses).SetupFirstOrDefault(new Course { Id = 5, SchoolId = 1 });
        Repo(m => m.CourseModules).SetupFirstOrDefault(new CourseModule { Id = 2, CourseId = 5 });

        // Sửa chương đã có (Id=2) nhưng SemesterNo = 3 (chỉ nhận 1 hoặc 2)
        var req = new SaveModuleRequest(Id: 2, CourseId: 5, Title: "Chương 1", Description: null,
            UnlockAfterDays: null, AvailableFrom: null, SemesterNo: 3);
        var result = await CreateSut().SaveModuleAsync(1, req);

        ShouldFail(result, "SEMESTER_INVALID");
    }

    [Fact]
    public async Task DeleteModule_DaCoTienDoHoc_TraVeHasProgress()
    {
        Repo(m => m.CourseModules).SetupFindOne(new CourseModule { Id = 2, CourseId = 5 });
        Repo(p => p.LessonProgresses).SetupAny(true);

        var result = await CreateSut().DeleteModuleAsync(1, 2);

        ShouldFail(result, "MODULE_HAS_PROGRESS");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. Lesson (bài học)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveLesson_ChuongKhongTonTai_TraVeModuleNotFound()
    {
        Repo(m => m.CourseModules).SetupFindOne(null);

        var req = new SaveLessonRequest(null, 2, "Bài 1", CourseLessonType.Article, "<p>Nội dung</p>", null, null, null);
        var result = await CreateSut().SaveLessonAsync(1, req);

        ShouldFail(result, "MODULE_NOT_FOUND");
    }

    [Fact]
    public async Task SaveLesson_VideoThieuUrl_TraVeVideoUrlRequired()
    {
        Repo(m => m.CourseModules).SetupFindOne(new CourseModule { Id = 2, CourseId = 5 });

        var req = new SaveLessonRequest(null, 2, "Bài video", CourseLessonType.Video, null, VideoUrl: null, null, null);
        var result = await CreateSut().SaveLessonAsync(1, req);

        ShouldFail(result, "VIDEO_URL_REQUIRED");
    }

    [Fact]
    public async Task SaveLesson_QuizThieuLienKet_TraVeQuizRequired()
    {
        Repo(m => m.CourseModules).SetupFindOne(new CourseModule { Id = 2, CourseId = 5 });

        var req = new SaveLessonRequest(null, 2, "Bài quiz", CourseLessonType.Quiz, null, null, null, QuizExamId: null);
        var result = await CreateSut().SaveLessonAsync(1, req);

        ShouldFail(result, "QUIZ_REQUIRED");
    }

    [Fact]
    public async Task SaveLesson_QuizKhongHopLe_TraVeQuizInvalid()
    {
        Repo(m => m.CourseModules).SetupFindOne(new CourseModule { Id = 2, CourseId = 5 });
        Repo(q => q.QuizExams).SetupAny(false); // đề trắc nghiệm không thuộc trường

        var req = new SaveLessonRequest(null, 2, "Bài quiz", CourseLessonType.Quiz, null, null, null, QuizExamId: 99);
        var result = await CreateSut().SaveLessonAsync(1, req);

        ShouldFail(result, "QUIZ_INVALID");
    }
}
