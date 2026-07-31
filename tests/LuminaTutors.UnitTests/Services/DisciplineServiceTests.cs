using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Discipline;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="DisciplineService"/> — ghi nhận/giải quyết/chuyển cấp vi phạm,
/// quét cổng (kèm tự sinh vi phạm "đi muộn").
/// </summary>
public class DisciplineServiceTests : ServiceTestBase
{
    private DisciplineService CreateSut() => new(Uow.Object, Mapper, NullLogger<DisciplineService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateRecord
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateRecord_HopLe_LuuVaTraVeDto()
    {
        var added = Repo(r => r.DisciplineRecords).CaptureAdds();
        // Sau khi lưu, service nạp lại kèm navigation để map DTO
        Repo(r => r.DisciplineRecords).SetupFindWithInclude(
            new DisciplineRecord { Id = 1, StudentId = 100, ViolationType = "Nói chuyện riêng", Student = Fake.User(id: 100) });

        var req = new CreateDisciplineRecordRequest(100, DateOnly.FromDateTime(DateTime.UtcNow), "Nói chuyện riêng", ViolationSeverity.Minor);
        var result = await CreateSut().CreateRecordAsync(1, reportedByUserId: 9, req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ViolationType.Should().Be("Nói chuyện riêng");
        added.Should().ContainSingle();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Resolve / Escalate
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveRecord_KhongTonTai_TraVeNotFound()
    {
        Repo(r => r.DisciplineRecords).SetupGetById(null);

        var result = await CreateSut().ResolveRecordAsync(1, "Nhắc nhở", 9);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task ResolveRecord_DaXuLy_TraVeAlreadyResolved()
    {
        Repo(r => r.DisciplineRecords).SetupGetById(new DisciplineRecord { Id = 1, Status = DisciplineStatus.Resolved });

        var result = await CreateSut().ResolveRecordAsync(1, "Nhắc nhở", 9);

        ShouldFail(result, "ALREADY_RESOLVED");
    }

    [Fact]
    public async Task ResolveRecord_HopLe_ChuyenTrangThaiResolved()
    {
        var record = new DisciplineRecord { Id = 1, Status = DisciplineStatus.Open };
        Repo(r => r.DisciplineRecords).SetupGetById(record);

        var result = await CreateSut().ResolveRecordAsync(1, "Mời phụ huynh", 9);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(DisciplineStatus.Resolved);
        record.ActionTaken.Should().Be("Mời phụ huynh");
        ShouldHaveSaved();
    }

    [Fact]
    public async Task EscalateRecord_HopLe_ChuyenCap()
    {
        var record = new DisciplineRecord { Id = 1, Status = DisciplineStatus.Open };
        Repo(r => r.DisciplineRecords).SetupGetById(record);

        var result = await CreateSut().EscalateRecordAsync(1, escalateToUserId: 7);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(DisciplineStatus.Escalated);
        record.EscalatedToUserId.Should().Be(7);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. RecordGateCheck
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordGateCheck_LoaiKhongHopLe_TraVeInvalidType()
    {
        var result = await CreateSut().RecordGateCheckAsync(1, 100, "KHONG_TON_TAI", 9, false, null);

        ShouldFail(result, "INVALID_TYPE");
    }

    [Fact]
    public async Task RecordGateCheck_KhongTre_ChiLuuLogCong()
    {
        var gateLogs   = Repo(g => g.GateCheckLogs).CaptureAdds();
        var discRecords = Repo(r => r.DisciplineRecords).CaptureAdds();

        var result = await CreateSut().RecordGateCheckAsync(1, 100, "In", 9, isLate: false, note: null);

        result.IsSuccess.Should().BeTrue();
        gateLogs.Should().ContainSingle();
        discRecords.Should().BeEmpty(); // không trễ ⇒ không sinh vi phạm
    }

    [Fact]
    public async Task RecordGateCheck_DiTre_TuSinhViPhamDiMuon()
    {
        var gateLogs    = Repo(g => g.GateCheckLogs).CaptureAdds();
        var discRecords = Repo(r => r.DisciplineRecords).CaptureAdds();

        var result = await CreateSut().RecordGateCheckAsync(1, 100, "In", 9, isLate: true, note: "Kẹt xe");

        result.IsSuccess.Should().BeTrue();
        gateLogs.Should().ContainSingle();
        discRecords.Should().ContainSingle();          // đi muộn ⇒ tự sinh 1 vi phạm
        discRecords[0].ViolationType.Should().Be("Đi muộn");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. GetDailyReport
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDailyReport_TongHopTheoMucDo()
    {
        Repo(r => r.DisciplineRecords).SetupFind(
            new DisciplineRecord { Id = 1, Severity = ViolationSeverity.Minor,  Student = Fake.User(id: 100) },
            new DisciplineRecord { Id = 2, Severity = ViolationSeverity.Severe, Student = Fake.User(id: 101) });
        Repo(g => g.GateCheckLogs).SetupFind(
            new GateCheckLog { Id = 1, CheckType = GateCheckType.In, IsLate = true },
            new GateCheckLog { Id = 2, CheckType = GateCheckType.Out });

        var result = await CreateSut().GetDailyReportAsync(1, DateOnly.FromDateTime(DateTime.UtcNow));

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalViolations.Should().Be(2);
        result.Data.MinorCount.Should().Be(1);
        result.Data.SevereCount.Should().Be(1);
        result.Data.LateArrivalsCount.Should().Be(1);
    }
}
