using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Learning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="VirtualLabService"/> — tạo/đóng phòng thí nghiệm 3D,
/// tra cứu theo mã phòng và kiểm tra hợp lệ môn học / loại thí nghiệm.
/// </summary>
public class VirtualLabServiceTests : ServiceTestBase
{
    private VirtualLabService CreateSut() => new(Uow.Object, NullLogger<VirtualLabService>.Instance);

    private static VirtualLabSession Session(int id = 1, int schoolId = 1, int teacherId = 50, bool active = true) =>
        new() { Id = id, SchoolId = schoolId, TeacherId = teacherId, IsActive = active,
                SessionName = "Chuẩn độ axit", SessionCode = "123456", SubjectTag = "chemistry", SceneType = "titration" };

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateSession
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_MonHocKhongHopLe_TraVeLoi()
    {
        var req = new CreateLabSessionRequest("Buổi học", "van_hoc", "titration");

        var result = await CreateSut().CreateSessionAsync(1, 50, req);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Môn học");
    }

    [Fact]
    public async Task Create_LoaiThiNghiemKhongHopLe_TraVeLoi()
    {
        var req = new CreateLabSessionRequest("Buổi học", "chemistry", "khong_ton_tai");

        var result = await CreateSut().CreateSessionAsync(1, 50, req);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("thí nghiệm");
    }

    [Fact]
    public async Task Create_HopLe_TaoPhongVaSinhMa()
    {
        Repo(s => s.VirtualLabSessions).SetupAny(false);          // mã chưa trùng
        var added = Repo(s => s.VirtualLabSessions).CaptureAdds();
        Repo(s => s.VirtualLabSessions).SetupGetById(Session()); // reload sau khi tạo

        var req = new CreateLabSessionRequest("Chuẩn độ axit-bazơ", "chemistry", "titration", MaxParticipants: 30);
        var result = await CreateSut().CreateSessionAsync(1, teacherId: 50, req);

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].SubjectTag.Should().Be("chemistry");
        added[0].MaxParticipants.Should().Be(30);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. GetByCode / GetById
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByCode_KhongTonTai_TraVeLoi()
    {
        Repo(s => s.VirtualLabSessions).SetupFindWithInclude();

        var result = await CreateSut().GetByCodeAsync(1, "999999");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCode_TonTai_TraVeDto()
    {
        Repo(s => s.VirtualLabSessions).SetupFindWithInclude(Session(id: 3));

        var result = await CreateSut().GetByCodeAsync(1, "123456");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(3);
        result.Data.SubjectTag.Should().Be("chemistry");
    }

    [Fact]
    public async Task GetById_SaiTruong_TraVeLoi()
    {
        Repo(s => s.VirtualLabSessions).SetupGetById(Session(schoolId: 99)); // thuộc trường khác

        var result = await CreateSut().GetByIdAsync(schoolId: 1, sessionId: 1);

        result.IsSuccess.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. CloseSession
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Close_KhongPhaiGiaoVienMoPhong_TraVeLoi()
    {
        Repo(s => s.VirtualLabSessions).SetupGetById(Session(teacherId: 50));

        var result = await CreateSut().CloseSessionAsync(1, 1, teacherId: 99); // GV khác

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Close_HopLe_DongPhong()
    {
        var session = Session(teacherId: 50, active: true);
        Repo(s => s.VirtualLabSessions).SetupGetById(session);

        var result = await CreateSut().CloseSessionAsync(1, 1, teacherId: 50);

        result.IsSuccess.Should().BeTrue();
        session.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }
}
