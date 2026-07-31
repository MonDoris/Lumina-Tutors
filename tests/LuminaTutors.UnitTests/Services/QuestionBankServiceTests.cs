using System.Net.Http;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="QuestionBankService"/> — CRUD câu hỏi và
/// kiểm tra định dạng file khi nhập câu hỏi từ Excel/Word/PDF.
/// (Phần đọc nội dung file thực tế thuộc phạm vi integration test.)
/// </summary>
public class QuestionBankServiceTests : ServiceTestBase
{
    private readonly Mock<IHttpClientFactory> _httpFactory = new();

    private QuestionBankService CreateSut() => new(
        Uow.Object, _httpFactory.Object, NullLogger<QuestionBankService>.Instance);

    private static IFormFile FileNamed(string name)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        return mock.Object;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CRUD
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_KhongTonTai_TraVeLoi()
    {
        Repo(q => q.QuestionBanks).SetupFindOne(null);

        var result = await CreateSut().GetByIdAsync(1, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_DangDungTrongDe_TraVeLoi()
    {
        Repo(q => q.QuestionBanks).SetupFindOne(new QuestionBank { Id = 5, SchoolId = 1 });
        Repo(eq => eq.QuizExamQuestions).SetupAny(true);

        var result = await CreateSut().DeleteAsync(1, 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đề thi");
    }

    [Fact]
    public async Task Delete_HopLe_Xoa()
    {
        var q = new QuestionBank { Id = 5, SchoolId = 1 };
        Repo(x => x.QuestionBanks).SetupFindOne(q);
        Repo(eq => eq.QuizExamQuestions).SetupAny(false);

        var result = await CreateSut().DeleteAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        Repo(x => x.QuestionBanks).Verify(r => r.Remove(q), Times.Once());
    }

    [Fact]
    public async Task Approve_HopLe_DanhDauDaDuyet()
    {
        var q = new QuestionBank { Id = 5, SchoolId = 1, IsApproved = false };
        Repo(x => x.QuestionBanks).SetupFindOne(q);

        var result = await CreateSut().ApproveAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        q.IsApproved.Should().BeTrue();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Kiểm tra định dạng file nhập câu hỏi
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportExcel_SaiDinhDang_TraVeLoi()
    {
        var result = await CreateSut().ImportFromExcelAsync(1, 50, 1, FileNamed("cauhoi.txt"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(".xlsx");
    }

    [Fact]
    public async Task ImportWord_SaiDinhDang_TraVeLoi()
    {
        var result = await CreateSut().ImportFromWordAsync(1, 50, 1, FileNamed("cauhoi.txt"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(".docx");
    }

    [Fact]
    public async Task ImportPdf_SaiDinhDang_TraVeLoi()
    {
        var result = await CreateSut().ImportFromPdfAsync(1, 50, 1, FileNamed("cauhoi.txt"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(".pdf");
    }

    [Fact]
    public async Task ImportPdf_SchoolIdKhongHopLe_TraVeLoi()
    {
        // Đúng định dạng .pdf nhưng schoolId = 0 (chưa đăng nhập) ⇒ chặn trước khi xử lý
        var result = await CreateSut().ImportFromPdfAsync(schoolId: 0, teacherId: 50, subjectId: 1, FileNamed("cauhoi.pdf"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("SchoolId");
    }
}
