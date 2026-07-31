using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Communication;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="NotificationService"/> — gửi thông báo theo nhóm người nhận,
/// đánh dấu đã đọc và đếm số chưa đọc.
/// </summary>
public class NotificationServiceTests : ServiceTestBase
{
    private NotificationService CreateSut() => new(Uow.Object, Mapper, NullLogger<NotificationService>.Instance);

    [Fact]
    public async Task Send_KhongCoNguoiNhan_TraVeNoTargets()
    {
        // Audience = Specific nhưng không truyền danh sách người nhận
        var req = new SendNotificationRequest("Thông báo", "Nội dung", NotificationType.General,
            TargetAudience: NotificationAudience.Specific, TargetUserIds: null);

        var result = await CreateSut().SendAsync(1, sentByUserId: 9, req);

        ShouldFail(result, "NO_TARGETS");
    }

    [Fact]
    public async Task Send_CoNguoiNhan_TaoThongBaoVaBanGhiNguoiNhan()
    {
        var notis = Repo(n => n.Notifications).CaptureAdds();
        Repo(r => r.NotificationRecipients);   // đảm bảo repo được gắn vào UoW trước khi service dùng
        var req = new SendNotificationRequest("Nghỉ học", "Ngày mai nghỉ", NotificationType.General,
            TargetAudience: NotificationAudience.Specific, TargetUserIds: new List<int> { 100, 101 });

        var result = await CreateSut().SendAsync(1, 9, req);

        result.IsSuccess.Should().BeTrue();
        notis.Should().ContainSingle();
        Repo(r => r.NotificationRecipients).Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<NotificationRecipient>>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task MarkRead_KhongTonTai_TraVeNotFound()
    {
        Repo(r => r.NotificationRecipients).SetupFind();

        var result = await CreateSut().MarkReadAsync(userId: 100, notificationId: 5);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task MarkRead_HopLe_DanhDauDaDoc()
    {
        var recipient = new NotificationRecipient { NotificationId = 5, UserId = 100, IsRead = false };
        Repo(r => r.NotificationRecipients).SetupFind(recipient);

        var result = await CreateSut().MarkReadAsync(100, 5);

        result.IsSuccess.Should().BeTrue();
        recipient.IsRead.Should().BeTrue();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task GetUnreadCount_TraVeSoLuong()
    {
        Repo(r => r.NotificationRecipients).SetupFind(
            new NotificationRecipient { UserId = 100, IsRead = false },
            new NotificationRecipient { UserId = 100, IsRead = false });

        var result = await CreateSut().GetUnreadCountAsync(100);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
    }

    [Fact]
    public async Task MarkAllRead_DanhDauTatCa()
    {
        var r1 = new NotificationRecipient { UserId = 100, IsRead = false };
        var r2 = new NotificationRecipient { UserId = 100, IsRead = false };
        Repo(r => r.NotificationRecipients).SetupFind(r1, r2);

        var result = await CreateSut().MarkAllReadAsync(100);

        result.IsSuccess.Should().BeTrue();
        new[] { r1, r2 }.Should().OnlyContain(x => x.IsRead);
    }
}
