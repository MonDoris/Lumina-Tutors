using LuminaTutors.Application.DTOs.Account;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="AccountService"/> — CRUD tài khoản (4 vai trò được quản lý)
/// cùng các quy tắc: kiểm tra quota, chống trùng email, và không cho khóa/xóa Admin cuối cùng.
///
/// LƯU Ý về mã lỗi: một số nhánh trong AccountService gọi
/// <c>Result.Failure("NOT_FOUND", "thông điệp")</c> — tức mã lỗi rơi vào ô Error thay vì ErrorCode.
/// Để test không phụ thuộc quy ước đảo này, ta kiểm tra mã xuất hiện ở BẤT KỲ ô nào
/// thông qua helper <see cref="FailedWithCode"/>.
/// </summary>
public class AccountServiceTests : ServiceTestBase
{
    private readonly Mock<IPasswordHasher<User>> _hasher = new();
    private readonly Mock<IQuotaService>          _quota  = new();

    private AccountService CreateSut() => new(
        Uow.Object, _hasher.Object, _quota.Object, NullLogger<AccountService>.Instance);

    /// <summary>Kiểm tra kết quả thất bại và mã lỗi (ở Error hoặc ErrorCode).</summary>
    private static void FailedWithCode<T>(Result<T> r, string code)
    {
        r.IsSuccess.Should().BeFalse();
        ($"{r.Error}|{r.ErrorCode}").Should().Contain(code);
    }
    private static void FailedWithCode(Result r, string code)
    {
        r.IsSuccess.Should().BeFalse();
        ($"{r.Error}|{r.ErrorCode}").Should().Contain(code);
    }

    private void AllowQuota() =>
        _quota.Setup(q => q.CanAddUserAsync(It.IsAny<int>(), It.IsAny<RoleCode>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Success(true));

    // ══════════════════════════════════════════════════════════════════════════
    //  1. GetAccountByIdAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAccountById_KhongTonTai_TraVeNotFound()
    {
        Repo(u => u.Users).SetupFind();

        var result = await CreateSut().GetAccountByIdAsync(schoolId: 1, userId: 5);

        FailedWithCode(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetAccountById_VaiTroKhongDuocQuanLy_TraVeForbidden()
    {
        var user = Fake.User(role: Fake.Role(id: 6, code: "ACCOUNTANT", name: "Kế toán"));
        Repo(u => u.Users).SetupFind(user);

        var result = await CreateSut().GetAccountByIdAsync(1, user.Id);

        FailedWithCode(result, "FORBIDDEN");
    }

    [Fact]
    public async Task GetAccountById_GiaoVien_TraVeChiTiet()
    {
        var teacher = Fake.User(id: 8, role: Fake.Role(id: 2, code: "TEACHER"));
        Repo(u => u.Users).SetupFind(teacher);

        var result = await CreateSut().GetAccountByIdAsync(1, 8);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be(8);
        result.Data.RoleCode.Should().Be("TEACHER");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. CreateAccountAsync — các nhánh kiểm tra đầu vào (trước transaction)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAccount_VaiTroKhongHopLe_TraVeInvalidRole()
    {
        var req = new CreateAccountRequest { RoleCode = "ACCOUNTANT", FullName = "A", Email = "a", Password = "12345678" };

        var result = await CreateSut().CreateAccountAsync(1, req);

        FailedWithCode(result, "INVALID_ROLE");
    }

    [Fact]
    public async Task CreateAccount_VuotQuota_TraVeLoiQuota()
    {
        _quota.Setup(q => q.CanAddUserAsync(1, RoleCode.Teacher, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Failure("Hết slot giáo viên", "QUOTA_EXCEEDED"));
        var req = new CreateAccountRequest { RoleCode = "TEACHER", FullName = "A", Email = "a", Password = "12345678" };

        var result = await CreateSut().CreateAccountAsync(1, req);

        FailedWithCode(result, "QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task CreateAccount_EmailDaTonTai_TraVeEmailExists()
    {
        AllowQuota();
        // Users.FindAsync trả về user có sẵn ⇒ vừa dùng cho tra admin, vừa khiến kiểm tra trùng email = có.
        Repo(u => u.Users).SetupFind(Fake.User(email: "teacher@ds.edu.vn"));
        var req = new CreateAccountRequest { RoleCode = "TEACHER", FullName = "A", Email = "teacher", Password = "12345678" };

        var result = await CreateSut().CreateAccountAsync(1, req);

        FailedWithCode(result, "EMAIL_EXISTS");
    }

    [Fact]
    public async Task CreateAccount_ThieuCauHinhVaiTro_TraVeConfigError()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind();  // không có admin, email chưa dùng
        Repo(r => r.Roles).SetupFind();  // không tìm thấy Role trong DB

        var req = new CreateAccountRequest { RoleCode = "TEACHER", FullName = "A", Email = "moi", Password = "12345678" };
        var result = await CreateSut().CreateAccountAsync(1, req);

        FailedWithCode(result, "CONFIG_ERROR");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. ToggleActiveAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToggleActive_KhongTonTai_TraVeNotFound()
    {
        Repo(u => u.Users).SetupFindOne(null);

        var result = await CreateSut().ToggleActiveAsync(1, 5);

        FailedWithCode(result, "NOT_FOUND");
    }

    [Fact]
    public async Task ToggleActive_AdminCuoiCung_KhongChoKhoa()
    {
        var admin = Fake.User(role: Fake.Role(id: 1, code: "ADMIN", name: "Nhà trường"), isActive: true);
        Repo(u => u.Users).SetupFindOne(admin);
        Repo(u => u.Users).SetupCount(1); // chỉ còn 1 admin active

        var result = await CreateSut().ToggleActiveAsync(1, admin.Id);

        FailedWithCode(result, "LAST_ADMIN");
        ShouldNotHaveSaved();
    }

    [Fact]
    public async Task ToggleActive_GiaoVien_DaoTrangThaiVaLuu()
    {
        var teacher = Fake.User(role: Fake.Role(id: 2, code: "TEACHER"), isActive: true);
        Repo(u => u.Users).SetupFindOne(teacher);

        var result = await CreateSut().ToggleActiveAsync(1, teacher.Id);

        result.IsSuccess.Should().BeTrue();
        teacher.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. ResetPasswordAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResetPassword_KhongTonTai_TraVeNotFound()
    {
        Repo(u => u.Users).SetupFindOne(null);

        var result = await CreateSut().ResetPasswordAsync(1, 5, "matkhaumoi");

        FailedWithCode(result, "NOT_FOUND");
    }

    [Fact]
    public async Task ResetPassword_HopLe_DoiHashVaLuu()
    {
        var user = Fake.User(passwordHash: "OLD");
        Repo(u => u.Users).SetupFindOne(user);
        _hasher.Setup(h => h.HashPassword(user, "matkhaumoi")).Returns("NEW_HASH");

        var result = await CreateSut().ResetPasswordAsync(1, user.Id, "matkhaumoi");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("NEW_HASH");
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. DeleteAccountAsync (soft delete = vô hiệu hóa)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_VaiTroKhongDuocQuanLy_TraVeForbidden()
    {
        var user = Fake.User(role: Fake.Role(id: 6, code: "ACCOUNTANT"));
        Repo(u => u.Users).SetupFindOne(user);

        var result = await CreateSut().DeleteAccountAsync(1, user.Id);

        FailedWithCode(result, "FORBIDDEN");
    }

    [Fact]
    public async Task Delete_AdminCuoiCung_KhongChoXoa()
    {
        var admin = Fake.User(role: Fake.Role(id: 1, code: "ADMIN"), isActive: true);
        Repo(u => u.Users).SetupFindOne(admin);
        Repo(u => u.Users).SetupCount(1);

        var result = await CreateSut().DeleteAccountAsync(1, admin.Id);

        FailedWithCode(result, "LAST_ADMIN");
    }

    [Fact]
    public async Task Delete_GiaoVien_VoHieuHoaVaLuu()
    {
        var teacher = Fake.User(role: Fake.Role(id: 2, code: "TEACHER"), isActive: true);
        Repo(u => u.Users).SetupFindOne(teacher);

        var result = await CreateSut().DeleteAccountAsync(1, teacher.Id);

        result.IsSuccess.Should().BeTrue();
        teacher.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  6. Danh sách phục vụ dropdown
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetClassSelectList_TraVeDanhSachSapXep()
    {
        Repo(c => c.Classes).SetupFind(Fake.Class(id: 2, name: "10A2"), Fake.Class(id: 1, name: "10A1"));

        var result = await CreateSut().GetClassSelectListAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data[0].ClassName.Should().Be("10A1"); // đã sắp theo tên
    }

    [Fact]
    public async Task GetSubjectSelectList_TraVeDanhSach()
    {
        Repo(s => s.Subjects).SetupFind(Fake.Subject(id: 1, name: "Toán", code: "TOAN"));

        var result = await CreateSut().GetSubjectSelectListAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        result.Data[0].SubjectCode.Should().Be("TOAN");
    }

    [Fact]
    public async Task GetTeacherPrimarySubjectId_TraVeSubjectIdChinh()
    {
        Repo(p => p.TeacherProfiles).SetupFind(Fake.TeacherProfile(userId: 50, primarySubjectId: 7));

        var subjectId = await CreateSut().GetTeacherPrimarySubjectIdAsync(1, 50);

        subjectId.Should().Be(7);
    }
}
