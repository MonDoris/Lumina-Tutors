using LuminaTutors.Application.DTOs.Quiz;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="QuizService"/> — ngân hàng câu hỏi (tạo/xóa/duyệt) và
/// đề thi trắc nghiệm (tạo/mở/đóng/xóa) với các ràng buộc nghiệp vụ.
/// </summary>
public class QuizServiceTests : ServiceTestBase
{
    private QuizService CreateSut() => new(Uow.Object, NullLogger<QuizService>.Instance);

    private static List<CreateOptionRequest> TwoOptions(int correctCount = 1) => new()
    {
        new('A', "Đáp án A", correctCount >= 1),
        new('B', "Đáp án B", correctCount >= 2)
    };

    private static CreateQuestionRequest QuestionReq(string text = "1 + 1 = ?", List<CreateOptionRequest>? options = null)
        => new(SubjectId: 1, QuestionText: text, QuestionType: "MultipleChoice", DifficultyLevel: "Easy",
               GradeLevelId: null, ChapterTag: null, ExplanationText: null, Options: options ?? TwoOptions());

    // ══════════════════════════════════════════════════════════════════════════
    //  1. Ngân hàng câu hỏi — validation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateQuestion_NoiDungRong_TraVeLoi()
    {
        var result = await CreateSut().CreateQuestionAsync(1, 50, QuestionReq(text: "   "));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Nội dung");
    }

    [Fact]
    public async Task CreateQuestion_KhongDungMotDapAnDung_TraVeLoi()
    {
        var result = await CreateSut().CreateQuestionAsync(1, 50, QuestionReq(options: TwoOptions(correctCount: 2)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("1 đáp án đúng");
    }

    [Fact]
    public async Task CreateQuestion_ItHonHaiLuaChon_TraVeLoi()
    {
        var oneOption = new List<CreateOptionRequest> { new('A', "Đáp án duy nhất", true) };
        var result = await CreateSut().CreateQuestionAsync(1, 50, QuestionReq(options: oneOption));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2 lựa chọn");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Ngân hàng câu hỏi — Delete / Approve
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteQuestion_KhongTonTai_TraVeLoi()
    {
        Repo(q => q.QuestionBanks).SetupGetById(null);

        var result = await CreateSut().DeleteQuestionAsync(1, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteQuestion_DangDuocDungTrongDe_TraVeLoi()
    {
        Repo(q => q.QuestionBanks).SetupGetById(new QuestionBank { Id = 5, SchoolId = 1 });
        Repo(eq => eq.QuizExamQuestions).SetupAny(true);

        var result = await CreateSut().DeleteQuestionAsync(1, 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đang được sử dụng");
    }

    [Fact]
    public async Task DeleteQuestion_HopLe_Xoa()
    {
        var q = new QuestionBank { Id = 5, SchoolId = 1 };
        Repo(x => x.QuestionBanks).SetupGetById(q);
        Repo(eq => eq.QuizExamQuestions).SetupAny(false);

        var result = await CreateSut().DeleteQuestionAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        Repo(x => x.QuestionBanks).Verify(r => r.Remove(q), Times.Once());
    }

    [Fact]
    public async Task ApproveQuestion_HopLe_DanhDauDaDuyet()
    {
        var q = new QuestionBank { Id = 5, SchoolId = 1, IsApproved = false };
        Repo(x => x.QuestionBanks).SetupGetById(q);

        var result = await CreateSut().ApproveQuestionAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        q.IsApproved.Should().BeTrue();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Đề thi
    // ══════════════════════════════════════════════════════════════════════════

    private static CreateQuizExamRequest ExamReq(string title, params int[] questionIds)
        => new(SubjectId: 1, GradeLevelId: null, Title: title, Description: null,
               TimeLimitMinutes: 45, PointsPerQuestion: 1, ShuffleQuestions: false, ShuffleOptions: false,
               ShowResultAfter: true, StartTime: null, EndTime: null, QuestionIds: questionIds);

    [Fact]
    public async Task CreateExam_TenRong_TraVeLoi()
    {
        var result = await CreateSut().CreateExamAsync(1, 50, ExamReq("  ", 1, 2));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tên đề thi");
    }

    [Fact]
    public async Task CreateExam_KhongChonCauHoi_TraVeLoi()
    {
        var result = await CreateSut().CreateExamAsync(1, 50, ExamReq("Đề KT 15 phút"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ít nhất 1 câu hỏi");
    }

    [Fact]
    public async Task CreateExam_CauHoiThieu_TraVeLoi()
    {
        // Yêu cầu 2 câu nhưng ngân hàng chỉ trả về 1 câu thuộc trường
        Repo(q => q.QuestionBanks).SetupFind(new QuestionBank { Id = 1, SchoolId = 1 });

        var result = await CreateSut().CreateExamAsync(1, 50, ExamReq("Đề KT", 1, 2));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("không tồn tại");
    }

    [Fact]
    public async Task PublishExam_DaDong_KhongMoLaiDuoc()
    {
        Repo(e => e.QuizExams).SetupGetById(new QuizExam { Id = 1, SchoolId = 1, Status = QuizExamStatus.Closed });

        var result = await CreateSut().PublishExamAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PublishExam_HopLe_ChuyenPublished()
    {
        var exam = new QuizExam { Id = 1, SchoolId = 1, Status = QuizExamStatus.Draft };
        Repo(e => e.QuizExams).SetupGetById(exam);

        var result = await CreateSut().PublishExamAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        exam.Status.Should().Be(QuizExamStatus.Published);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task CloseExam_HopLe_ChuyenClosed()
    {
        var exam = new QuizExam { Id = 1, SchoolId = 1, Status = QuizExamStatus.Published };
        Repo(e => e.QuizExams).SetupGetById(exam);

        var result = await CreateSut().CloseExamAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        exam.Status.Should().Be(QuizExamStatus.Closed);
    }

    [Fact]
    public async Task DeleteExam_DangMo_TraVeLoi()
    {
        Repo(e => e.QuizExams).SetupGetById(new QuizExam { Id = 1, SchoolId = 1, Status = QuizExamStatus.Published });

        var result = await CreateSut().DeleteExamAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đang mở");
    }

    [Fact]
    public async Task DeleteExam_DaDong_XoaKemBaiLam()
    {
        Repo(e => e.QuizExams).SetupGetById(new QuizExam { Id = 1, SchoolId = 1, Status = QuizExamStatus.Closed });
        Repo(a => a.StudentQuizAttempts).SetupFindNoInclude(); // không có bài làm

        var result = await CreateSut().DeleteExamAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        Repo(e => e.QuizExams).Verify(r => r.Remove(It.IsAny<QuizExam>()), Times.Once());
    }
}
