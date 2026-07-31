using LuminaTutors.Application.DTOs.OnlineClassroom;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="OnlineClassroomService"/> — quản lý phòng học WebRTC:
/// sửa/xóa/bắt đầu/kết thúc với ràng buộc trạng thái và quyền của giáo viên tạo phòng.
/// </summary>
public class OnlineClassroomServiceTests : ServiceTestBase
{
    private OnlineClassroomService CreateSut() => new(Uow.Object, NullLogger<OnlineClassroomService>.Instance);

    private static OnlineSession Session(OnlineSessionStatus status = OnlineSessionStatus.Scheduled, int teacherId = 50) =>
        new() { Id = 1, SchoolId = 1, TeacherId = teacherId, Status = status, Title = "Ôn tập" };

    [Fact]
    public async Task Delete_PhongDangDienRa_TraVeLoi()
    {
        Repo(x => x.OnlineSessions).SetupFindOne(Session(OnlineSessionStatus.Live));

        var result = await CreateSut().DeleteAsync(1, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đang diễn ra");
    }

    [Fact]
    public async Task StartSession_KhongPhaiGiaoVienTao_TraVeLoi()
    {
        Repo(x => x.OnlineSessions).SetupFindOne(Session(teacherId: 50));

        var result = await CreateSut().StartSessionAsync(1, 1, teacherId: 99); // GV khác

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("giáo viên tạo phòng");
    }

    [Fact]
    public async Task StartSession_PhongDaKetThuc_TraVeLoi()
    {
        Repo(x => x.OnlineSessions).SetupFindOne(Session(OnlineSessionStatus.Ended));

        var result = await CreateSut().StartSessionAsync(1, 1, 50);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đã kết thúc");
    }

    [Fact]
    public async Task StartSession_HopLe_ChuyenLive()
    {
        var s = Session(OnlineSessionStatus.Scheduled);
        Repo(x => x.OnlineSessions).SetupFindOne(s); // dùng cho cả bước cập nhật lẫn GetById reload

        var result = await CreateSut().StartSessionAsync(1, 1, 50);

        result.IsSuccess.Should().BeTrue();
        s.Status.Should().Be(OnlineSessionStatus.Live);
        s.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_PhongDaKetThuc_TraVeLoi()
    {
        Repo(x => x.OnlineSessions).SetupFindOne(Session(OnlineSessionStatus.Ended));

        var req = new UpdateOnlineSessionRequest("Tên mới", null, null, 50);
        var result = await CreateSut().UpdateAsync(1, 1, req);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đã kết thúc");
    }
}
