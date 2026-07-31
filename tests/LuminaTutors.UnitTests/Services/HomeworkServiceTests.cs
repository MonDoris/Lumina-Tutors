using LuminaTutors.Application.DTOs.Homework;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="HomeworkService"/> — giáo viên tạo/sửa/xóa bài tập, chấm điểm;
/// học sinh nộp bài (kèm ràng buộc quyền phân công, quá hạn).
/// </summary>
public class HomeworkServiceTests : ServiceTestBase
{
    private HomeworkService CreateSut() => new(Uow.Object, NullLogger<HomeworkService>.Instance);

    private static CreateAssignmentRequest NewAssignmentReq(bool published = true) =>
        new(SubjectAssignmentId: 1, Title: "Bài tập chương 1", Instructions: "Làm bài 1-5",
            AssignmentType: AssignmentType.Homework, MaxScore: 10, DueDate: DateTime.UtcNow.AddDays(3),
            AllowLateSubmission: false, LatePenaltyPercent: 0, IsPublished: published);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. Giáo viên: tạo / sửa / xóa / chấm
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAssignment_PhanCongKhongHopLe_TraVeLoi()
    {
        Repo(sa => sa.SubjectAssignments).SetupFindOne(null);

        var result = await CreateSut().CreateAssignmentAsync(1, teacherId: 50, NewAssignmentReq());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAssignment_HopLe_TraVeId()
    {
        Repo(sa => sa.SubjectAssignments).SetupFindOne(new SubjectAssignment { Id = 1, SchoolId = 1, TeacherId = 50 });
        var added = Repo(a => a.Assignments).CaptureAdds();

        var result = await CreateSut().CreateAssignmentAsync(1, 50, NewAssignmentReq());

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].Title.Should().Be("Bài tập chương 1");
        ShouldHaveSaved();
    }

    [Fact]
    public async Task UpdateAssignment_KhongTonTai_TraVeLoi()
    {
        Repo(a => a.Assignments).SetupFindOne(null);

        var result = await CreateSut().UpdateAssignmentAsync(1, 5,
            new UpdateAssignmentRequest("T", null, AssignmentType.Homework, 10, null, false, 0, true));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAssignment_HopLe_CapNhat()
    {
        var a = new Assignment { Id = 5, SchoolId = 1, Title = "Cũ", IsPublished = false };
        Repo(x => x.Assignments).SetupFindOne(a);

        var result = await CreateSut().UpdateAssignmentAsync(1, 5,
            new UpdateAssignmentRequest("Tiêu đề mới", null, AssignmentType.Quiz, 20, null, true, 10, true));

        result.IsSuccess.Should().BeTrue();
        a.Title.Should().Be("Tiêu đề mới");
        a.IsPublished.Should().BeTrue();
        a.PublishedAt.Should().NotBeNull(); // vừa publish lần đầu
        ShouldHaveSaved();
    }

    [Fact]
    public async Task DeleteAssignment_HopLe_Xoa()
    {
        var a = new Assignment { Id = 5, SchoolId = 1 };
        Repo(x => x.Assignments).SetupFindOne(a);

        var result = await CreateSut().DeleteAssignmentAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        Repo(x => x.Assignments).Verify(r => r.Remove(a), Times.Once());
    }

    [Fact]
    public async Task GradeSubmission_KhongTonTai_TraVeLoi()
    {
        Repo(s => s.AssignmentSubmissions).SetupGetById(null);

        var result = await CreateSut().GradeSubmissionAsync(1, new GradeSubmissionRequest(8, "Tốt"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GradeSubmission_HopLe_ChamDiemVaChuyenGraded()
    {
        var sub = new AssignmentSubmission { Id = 1, SubmissionStatus = SubmissionStatus.Submitted };
        Repo(s => s.AssignmentSubmissions).SetupGetById(sub);

        var result = await CreateSut().GradeSubmissionAsync(1, new GradeSubmissionRequest(8.5m, "Khá tốt"));

        result.IsSuccess.Should().BeTrue();
        sub.Score.Should().Be(8.5m);
        sub.SubmissionStatus.Should().Be(SubmissionStatus.Graded);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Học sinh: nộp bài
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Submit_BaiTapKhongTonTai_TraVeLoi()
    {
        Repo(a => a.Assignments).SetupFindOne(null);

        var result = await CreateSut().SubmitAssignmentAsync(1, 100, 5, "Bài làm", NoFiles());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_QuaHanVaKhongChoNopMuon_TraVeLoi()
    {
        Repo(a => a.Assignments).SetupFindOne(new Assignment
        {
            Id = 5, SchoolId = 1, IsPublished = true,
            DueDate = DateTime.UtcNow.AddDays(-1), AllowLateSubmission = false
        });

        var result = await CreateSut().SubmitAssignmentAsync(1, 100, 5, "Bài làm", NoFiles());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("quá hạn");
    }

    [Fact]
    public async Task Submit_HopLe_TaoBaiNop()
    {
        Repo(a => a.Assignments).SetupFindOne(new Assignment
        {
            Id = 5, SchoolId = 1, IsPublished = true, DueDate = DateTime.UtcNow.AddDays(3)
        });
        Repo(s => s.AssignmentSubmissions).SetupFindOne(null); // chưa có bài nộp ⇒ tạo mới
        var added = Repo(s => s.AssignmentSubmissions).CaptureAdds();

        var result = await CreateSut().SubmitAssignmentAsync(1, studentId: 100, assignmentId: 5, "Bài làm của em", NoFiles());

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].StudentId.Should().Be(100);
    }

    // Không có file đính kèm (tránh thao tác I/O đĩa trong unit test).
    private static IEnumerable<IFormFile> NoFiles() => new List<IFormFile>();
}
