using LuminaTutors.Application.Services;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="SupportChatService"/> — kênh hỗ trợ giữa trường và Quản trị hệ thống.
/// </summary>
public class SupportChatServiceTests : ServiceTestBase
{
    private SupportChatService CreateSut() => new(Uow.Object);

    [Fact]
    public async Task GetSchoolThread_TruongKhongTonTai_TraVeNotFound()
    {
        Repo(s => s.Schools).SetupGetById(null);

        var result = await CreateSut().GetSchoolThreadAsync(schoolId: 1, markReadForSysAdmin: false);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Send_NoiDungTrong_TraVeEmpty()
    {
        var result = await CreateSut().SendAsync(1, senderUserId: 9, text: "   ");

        ShouldFail(result, "EMPTY");
    }

    [Fact]
    public async Task Send_ChuaCoSysAdmin_TraVeNoSysAdmin()
    {
        Repo(u => u.Users).SetupFind(); // không tìm thấy tài khoản SYSADMIN

        var result = await CreateSut().SendAsync(1, 9, "Cần hỗ trợ gấp");

        ShouldFail(result, "NO_SYSADMIN");
    }
}
