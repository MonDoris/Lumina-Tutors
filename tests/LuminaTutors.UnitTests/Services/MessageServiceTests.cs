using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Communication;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="MessageService"/> — nhắn tin 1-1, với ràng buộc quyền:
/// chỉ được xóa tin nhắn của chính mình.
/// </summary>
public class MessageServiceTests : ServiceTestBase
{
    private MessageService CreateSut() => new(Uow.Object, Mapper, NullLogger<MessageService>.Instance);

    [Fact]
    public async Task DeleteMessage_KhongTonTai_TraVeNotFound()
    {
        Repo(m => m.Messages).SetupGetById(null);

        var result = await CreateSut().DeleteMessageAsync(1, requestedByUserId: 9);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task DeleteMessage_KhongPhaiNguoiGui_TraVeForbidden()
    {
        Repo(m => m.Messages).SetupGetById(new Message { Id = 1, SenderId = 9 });

        var result = await CreateSut().DeleteMessageAsync(1, requestedByUserId: 99); // người khác

        ShouldFail(result, "FORBIDDEN");
    }

    [Fact]
    public async Task DeleteMessage_NguoiGui_XoaMem()
    {
        var msg = new Message { Id = 1, SenderId = 9, MessageText = "Xin chào" };
        Repo(m => m.Messages).SetupGetById(msg);

        var result = await CreateSut().DeleteMessageAsync(1, requestedByUserId: 9);

        result.IsSuccess.Should().BeTrue();
        msg.IsDeleted.Should().BeTrue();
        msg.MessageText.Should().Be("[Tin nhắn đã bị xóa]");
        ShouldHaveSaved();
    }
}
