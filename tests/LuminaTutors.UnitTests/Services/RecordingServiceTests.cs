using LuminaTutors.Application.DTOs.Recording;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="RecordingService"/> — lưu và liệt kê bản ghi buổi học.
/// </summary>
public class RecordingServiceTests : ServiceTestBase
{
    private RecordingService CreateSut() => new(Uow.Object);

    [Fact]
    public async Task Save_ThieuFileUrl_TraVeLoi()
    {
        var input = new SaveRecordingInput { FileUrl = "  ", Source = RecordingSource.Online };

        var result = await CreateSut().SaveAsync(1, input);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Save_HopLe_LuuBanGhi()
    {
        var added = Repo(r => r.SessionRecordings).CaptureAdds();
        var input = new SaveRecordingInput
        {
            FileUrl = "/rec/abc.webm", Source = RecordingSource.Online, TeacherId = 50,
            StartedAt = DateTime.UtcNow.AddMinutes(-40), EndedAt = DateTime.UtcNow, ParticipantCount = 12
        };

        var result = await CreateSut().SaveAsync(1, input);

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].FileUrl.Should().Be("/rec/abc.webm");
    }

    [Fact]
    public async Task GetAll_TraVeDanhSachTinhThoiLuong()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        Repo(r => r.SessionRecordings).SetupFind(new SessionRecording
        {
            Id = 1, SchoolId = 1, Source = RecordingSource.Online,
            StartedAt = start, EndedAt = start.AddMinutes(30), FileUrl = "/rec/a.webm"
        });

        var result = await CreateSut().GetAllAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        result.Data[0].DurationSeconds.Should().Be(1800); // 30 phút
    }
}
