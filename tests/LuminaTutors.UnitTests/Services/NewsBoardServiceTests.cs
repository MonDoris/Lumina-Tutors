using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Communication;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="NewsBoardService"/> — đăng bảng tin, công bố (publish) và xóa
/// (kèm ràng buộc quyền: chỉ người đăng mới được xóa).
/// </summary>
public class NewsBoardServiceTests : ServiceTestBase
{
    private NewsBoardService CreateSut() => new(Uow.Object, Mapper, NullLogger<NewsBoardService>.Instance);

    [Fact]
    public async Task Create_HopLe_LuuVaTraVeDto()
    {
        var added = Repo(n => n.NewsBoards).CaptureAdds();

        var req = new CreateNewsRequest("Thông báo tuyển sinh", "<p>Nội dung</p>", PublishNow: true);
        var result = await CreateSut().CreateAsync(1, publishedByUserId: 9, req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Thông báo tuyển sinh");
        added.Should().ContainSingle();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Publish_KhongTonTai_TraVeNotFound()
    {
        Repo(n => n.NewsBoards).SetupGetById(null);

        var result = await CreateSut().PublishAsync(5, 9);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Publish_DaCongBo_TraVeAlreadyPublished()
    {
        Repo(n => n.NewsBoards).SetupGetById(new NewsBoard { Id = 5, IsPublished = true });

        var result = await CreateSut().PublishAsync(5, 9);

        ShouldFail(result, "ALREADY_PUBLISHED");
    }

    [Fact]
    public async Task Publish_HopLe_CongBo()
    {
        var post = new NewsBoard { Id = 5, IsPublished = false };
        Repo(n => n.NewsBoards).SetupGetById(post);

        var result = await CreateSut().PublishAsync(5, publishedByUserId: 9);

        result.IsSuccess.Should().BeTrue();
        post.IsPublished.Should().BeTrue();
        post.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_KhongPhaiNguoiDang_TraVeForbidden()
    {
        Repo(n => n.NewsBoards).SetupGetById(new NewsBoard { Id = 5, PublishedByUserId = 9 });

        var result = await CreateSut().DeleteAsync(5, requestedByUserId: 99); // người khác

        ShouldFail(result, "FORBIDDEN");
    }

    [Fact]
    public async Task Delete_NguoiDang_Xoa()
    {
        var post = new NewsBoard { Id = 5, PublishedByUserId = 9 };
        Repo(n => n.NewsBoards).SetupGetById(post);

        var result = await CreateSut().DeleteAsync(5, requestedByUserId: 9);

        result.IsSuccess.Should().BeTrue();
        Repo(n => n.NewsBoards).Verify(r => r.Remove(post), Times.Once());
    }
}
