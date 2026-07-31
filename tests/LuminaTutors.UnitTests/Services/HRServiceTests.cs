using LuminaTutors.Application.DTOs.HR;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.HR;
using LuminaTutors.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="HRService"/> — quản lý giáo viên, hợp đồng và bảng lương
/// (kèm ràng buộc quota, trùng mã, trùng kỳ lương).
/// </summary>
public class HRServiceTests : ServiceTestBase
{
    private readonly Mock<IPasswordHasher<User>> _hasher = new();
    private readonly Mock<IQuotaService>          _quota  = new();

    private HRService CreateSut() => new(
        Uow.Object, Mapper, _hasher.Object, _quota.Object, NullLogger<HRService>.Instance);

    private void AllowQuota() =>
        _quota.Setup(q => q.CanAddUserAsync(It.IsAny<int>(), RoleCode.Teacher, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Success(true));

    private static CreateTeacherRequest NewTeacherReq() =>
        new("Nguyễn Văn GV", "gv", "GV001", null, null, Gender.Male, null, "Toán", null);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. Giáo viên
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateTeacher_VuotQuota_TraVeLoiQuota()
    {
        _quota.Setup(q => q.CanAddUserAsync(1, RoleCode.Teacher, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Failure("Hết slot", "QUOTA_EXCEEDED"));

        var result = await CreateSut().CreateTeacherAsync(1, NewTeacherReq());

        ShouldFail(result, "QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task CreateTeacher_EmailTrung_TraVeEmailExists()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind(Fake.User(email: "gv@ds.edu.vn"));

        var result = await CreateSut().CreateTeacherAsync(1, NewTeacherReq());

        ShouldFail(result, "EMAIL_EXISTS");
    }

    [Fact]
    public async Task CreateTeacher_ThieuVaiTro_TraVeConfigError()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind();
        Repo(r => r.Roles).SetupFind();

        var result = await CreateSut().CreateTeacherAsync(1, NewTeacherReq());

        ShouldFail(result, "CONFIG_ERROR");
    }

    [Fact]
    public async Task GetTeacherById_KhongTonTai_TraVeNotFound()
    {
        Repo(t => t.TeacherProfiles).SetupFindWithInclude();

        var result = await CreateSut().GetTeacherByIdAsync(1, 5);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetTeacherById_TonTai_TraVeChiTiet()
    {
        Repo(t => t.TeacherProfiles).SetupFindWithInclude(Fake.TeacherProfile(userId: 50, code: "GV001"));

        var result = await CreateSut().GetTeacherByIdAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TeacherCode.Should().Be("GV001");
    }

    [Fact]
    public async Task UpdateTeacher_TrungMaGiaoVien_TraVeCodeExists()
    {
        var profile = Fake.TeacherProfile(userId: 50, code: "GV001");
        Repo(t => t.TeacherProfiles).SetupFindWithInclude(profile);
        Repo(t => t.TeacherProfiles).SetupAny(true); // mã mới đã tồn tại ở hồ sơ khác

        var req = new UpdateTeacherRequest("Tên mới", "GV999", null, null, Gender.Male, null, null, null);
        var result = await CreateSut().UpdateTeacherAsync(1, 1, req);

        ShouldFail(result, "CODE_EXISTS");
    }

    [Fact]
    public async Task DeactivateTeacher_HopLe_TatHoatDong()
    {
        var profile = Fake.TeacherProfile(userId: 50);
        Repo(t => t.TeacherProfiles).SetupFindWithInclude(profile);

        var result = await CreateSut().DeactivateTeacherAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        profile.User.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Hợp đồng
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateContract_GiaoVienKhongTonTai_TraVeNotFound()
    {
        Repo(t => t.TeacherProfiles).SetupGetById(null);

        var req = new CreateContractRequest(1, "HD001", ContractType.FullTime,
            new DateOnly(2026, 1, 1), null, 10_000_000, null, null);
        var result = await CreateSut().CreateContractAsync(1, 9, req);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task CreateContract_HopLe_LuuHopDong()
    {
        Repo(t => t.TeacherProfiles).SetupGetById(Fake.TeacherProfile(userId: 50));
        var added = Repo(c => c.TeacherContracts).CaptureAdds();

        var req = new CreateContractRequest(1, "HD001", ContractType.FullTime,
            new DateOnly(2026, 1, 1), null, 12_000_000, null, null);
        var result = await CreateSut().CreateContractAsync(1, 9, req);

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].BaseSalary.Should().Be(12_000_000);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Bảng lương
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreatePayroll_TrungKy_TraVeDuplicate()
    {
        Repo(t => t.TeacherProfiles).SetupGetById(Fake.TeacherProfile(userId: 50));
        Repo(p => p.Payrolls).SetupFind(new Payroll { UserId = 50, PayrollMonth = 1, PayrollYear = 2026 });

        var req = new CreatePayrollRequest(1, 1, 2026, 10_000_000);
        var result = await CreateSut().CreatePayrollAsync(1, 9, req);

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task CreatePayroll_HopLe_TinhLuongVaTraVeDto()
    {
        Repo(t => t.TeacherProfiles).SetupGetById(Fake.TeacherProfile(userId: 50));
        Repo(p => p.Payrolls).SetupFind(); // chưa có bảng lương
        var added = Repo(p => p.Payrolls).CaptureAdds();

        // Lương gộp = 10tr + 2tr phụ cấp = 12tr; trừ 1tr BHXH ⇒ thực nhận 11tr
        var req = new CreatePayrollRequest(1, 1, 2026, 10_000_000,
            TeachingAllowance: 2_000_000, InsuranceDeduction: 1_000_000);
        var result = await CreateSut().CreatePayrollAsync(1, 9, req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.GrossIncome.Should().Be(12_000_000);
        result.Data.NetSalary.Should().Be(11_000_000);
        added.Should().ContainSingle();
    }

    [Fact]
    public async Task ApprovePayroll_KhongTonTai_TraVeNotFound()
    {
        Repo(p => p.Payrolls).SetupGetById(null);

        var result = await CreateSut().ApprovePayrollAsync(1, 9);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task ApprovePayroll_DaDuyet_TraVeAlreadyApproved()
    {
        Repo(p => p.Payrolls).SetupGetById(new Payroll { Id = 1, Status = PayrollStatus.Approved });

        var result = await CreateSut().ApprovePayrollAsync(1, 9);

        ShouldFail(result, "ALREADY_APPROVED");
    }

    [Fact]
    public async Task ApprovePayroll_HopLe_DuyetVaLuu()
    {
        var payroll = new Payroll { Id = 1, Status = PayrollStatus.Draft };
        Repo(p => p.Payrolls).SetupGetById(payroll);

        var result = await CreateSut().ApprovePayrollAsync(1, approvedByUserId: 9);

        result.IsSuccess.Should().BeTrue();
        payroll.Status.Should().Be(PayrollStatus.Approved);
        payroll.ApprovedByUserId.Should().Be(9);
        ShouldHaveSaved();
    }
}
