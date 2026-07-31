using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="AuthService"/> — đăng nhập, đổi mật khẩu, đăng xuất,
/// luồng lời mời (invite) và khôi phục mật khẩu qua số điện thoại.
///
/// Cách tổ chức: kế thừa <see cref="ServiceTestBase"/> để có sẵn UoW mock + repo mock.
/// Các phụ thuộc riêng của AuthService (hasher, quota) được mock cục bộ tại đây.
/// </summary>
public class AuthServiceTests : ServiceTestBase
{
    // ─── Phụ thuộc riêng của AuthService ──────────────────────────────────────
    private readonly Mock<IPasswordHasher<User>> _hasher = new();
    private readonly Mock<IQuotaService>          _quota  = new();

    /// <summary>Dựng service cần test với đúng các phụ thuộc (SUT = System Under Test).</summary>
    private AuthService CreateSut() => new(
        Uow.Object,
        Mapper,
        _hasher.Object,
        new Mock<IConfiguration>().Object,
        NullLogger<AuthService>.Instance,
        _quota.Object);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. LoginAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_DungThongTin_TraVeThanhCong_VaCapNhatLanDangNhap()
    {
        // Arrange — có một user active, mật khẩu khớp
        var user = Fake.User(id: 5, email: "teacher@ds.edu.vn", passwordHash: "HASH");
        Repo(u => u.Users).SetupFind(user);
        _hasher.Setup(h => h.VerifyHashedPassword(user, "HASH", "matkhau"))
               .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await CreateSut().LoginAsync(new LoginRequest("Teacher@DS.edu.vn", "matkhau"));

        // Assert — trả về hồ sơ đăng nhập đúng và đã lưu thời điểm đăng nhập
        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be(5);
        result.Data.RoleCode.Should().Be("TEACHER");
        result.Data.SchoolName.Should().Be("Trường THPT Đông Sơn");
        user.LastLoginAt.Should().NotBeNull();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Login_SaiMatKhau_TraVeLoi_KhongLuu()
    {
        var user = Fake.User(passwordHash: "HASH");
        Repo(u => u.Users).SetupFind(user);
        _hasher.Setup(h => h.VerifyHashedPassword(user, "HASH", It.IsAny<string>()))
               .Returns(PasswordVerificationResult.Failed);

        var result = await CreateSut().LoginAsync(new LoginRequest("teacher@ds.edu.vn", "sai"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID");
        ShouldNotHaveSaved();
    }

    [Fact]
    public async Task Login_EmailKhongTonTai_TraVeLoi()
    {
        Repo(u => u.Users).SetupFind(); // repo rỗng → không tìm thấy user

        var result = await CreateSut().LoginAsync(new LoginRequest("khong@co.vn", "matkhau"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID");
    }

    [Fact]
    public async Task Login_TaiKhoanBiKhoa_TraVeLoi_KhongKiemTraMatKhau()
    {
        var user = Fake.User(isActive: false);
        Repo(u => u.Users).SetupFind(user);

        var result = await CreateSut().LoginAsync(new LoginRequest("teacher@ds.edu.vn", "matkhau"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID");
        // Tài khoản khóa thì không cần (và không được) kiểm tra mật khẩu
        _hasher.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never());
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. GetCurrentUserAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCurrentUser_TonTai_TraVeThongTin()
    {
        var user = Fake.User(id: 7, fullName: "Trần Thị B");
        Repo(u => u.Users).SetupFind(user);

        var result = await CreateSut().GetCurrentUserAsync(7);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be(7);
        result.Data.FullName.Should().Be("Trần Thị B");
    }

    [Fact]
    public async Task GetCurrentUser_KhongTonTai_TraVeNotFound()
    {
        Repo(u => u.Users).SetupFind();

        var result = await CreateSut().GetCurrentUserAsync(999);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. ChangePasswordAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChangePassword_XacNhanKhongKhop_TraVeLoi()
    {
        var request = new ChangePasswordRequest("cu", "matkhaumoi", "khac");

        var result = await CreateSut().ChangePasswordAsync(1, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PASS_MISMATCH");
        ShouldNotHaveSaved();
    }

    [Fact]
    public async Task ChangePassword_SaiMatKhauHienTai_TraVeLoi()
    {
        var user = Fake.User(passwordHash: "OLD");
        Repo(u => u.Users).SetupGetById(user);
        _hasher.Setup(h => h.VerifyHashedPassword(user, "OLD", "sai"))
               .Returns(PasswordVerificationResult.Failed);

        var result = await CreateSut().ChangePasswordAsync(1, new ChangePasswordRequest("sai", "matkhaumoi", "matkhaumoi"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID");
        ShouldNotHaveSaved();
    }

    [Fact]
    public async Task ChangePassword_HopLe_DoiHashVaLuu()
    {
        var user = Fake.User(passwordHash: "OLD");
        Repo(u => u.Users).SetupGetById(user);
        _hasher.Setup(h => h.VerifyHashedPassword(user, "OLD", "cu")).Returns(PasswordVerificationResult.Success);
        _hasher.Setup(h => h.HashPassword(user, "matkhaumoi")).Returns("NEW_HASH");

        var result = await CreateSut().ChangePasswordAsync(1, new ChangePasswordRequest("cu", "matkhaumoi", "matkhaumoi"));

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("NEW_HASH");
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. LogoutAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Logout_CoRefreshToken_ThuHoiTokenVaLuu()
    {
        var token = new RefreshToken { UserId = 1, Token = "tok", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        Repo(u => u.RefreshTokens).SetupFind(token);

        var result = await CreateSut().LogoutAsync(1, "tok");

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Logout_KhongCoToken_VanThanhCong()
    {
        var result = await CreateSut().LogoutAsync(1, string.Empty);

        result.IsSuccess.Should().BeTrue();
        ShouldNotHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. Invite Link — tạo / tra cứu / thu hồi
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateInviteLink_VaiTroKhongHopLe_TraVeNotFound()
    {
        Repo(r => r.Roles).SetupGetById(null); // role không tồn tại

        var result = await CreateSut().CreateInviteLinkAsync(
            schoolId: 1, createdByUserId: 1, new CreateInviteLinkRequest(99, "a@b.vn", null));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CreateInviteLink_HopLe_LuuVaTraVeDto()
    {
        Repo(r => r.Roles).SetupGetById(Fake.Role(id: 2));
        var added = Repo(i => i.InviteLinks).CaptureAdds();
        // Sau khi lưu, service nạp lại invite kèm navigation → trả về từ FindAsync
        Repo(i => i.InviteLinks).SetupFind(Fake.Invite(id: 10, targetRoleId: 2));

        var result = await CreateSut().CreateInviteLinkAsync(
            schoolId: 1, createdByUserId: 1, new CreateInviteLinkRequest(2, "moi@ds.edu.vn", null, ExpiryHours: 24));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].SchoolId.Should().Be(1);
        added[0].TargetEmail.Should().Be("moi@ds.edu.vn");
        ShouldHaveSaved();
    }

    [Fact]
    public async Task GetInviteLinkByToken_KhongTonTai_TraVeNotFound()
    {
        Repo(i => i.InviteLinks).SetupFind(); // rỗng

        var result = await CreateSut().GetInviteLinkByTokenAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetInviteLinkByToken_DaHetHan_TraVeLoiInvalid()
    {
        var expired = Fake.Invite(expiresAt: DateTime.UtcNow.AddDays(-1)); // hết hạn → IsValid = false
        Repo(i => i.InviteLinks).SetupFind(expired);

        var result = await CreateSut().GetInviteLinkByTokenAsync(expired.Token);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVITE_INVALID");
    }

    [Fact]
    public async Task GetInviteLinkByToken_ConHieuLuc_TraVeDto()
    {
        var invite = Fake.Invite(id: 10);
        Repo(i => i.InviteLinks).SetupFind(invite);

        var result = await CreateSut().GetInviteLinkByTokenAsync(invite.Token);

        result.IsSuccess.Should().BeTrue();
        result.Data!.InviteId.Should().Be(10);
        result.Data.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeInviteLink_DaSuDung_KhongChoThuHoi()
    {
        var used = Fake.Invite(usedAt: DateTime.UtcNow);
        Repo(i => i.InviteLinks).SetupGetById(used);

        var result = await CreateSut().RevokeInviteLinkAsync(used.Id, revokedByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVITE_USED");
    }

    [Fact]
    public async Task RevokeInviteLink_HopLe_DanhDauThuHoiVaLuu()
    {
        var invite = Fake.Invite();
        Repo(i => i.InviteLinks).SetupGetById(invite);

        var result = await CreateSut().RevokeInviteLinkAsync(invite.Id, revokedByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        invite.IsRevoked.Should().BeTrue();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  6. ActivateInviteAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ActivateInvite_MatKhauKhongKhop_TraVeLoi()
    {
        var request = new ActivateInviteRequest(Guid.NewGuid(), "Nguyễn Văn C", "0900000000", "matkhau1", "khac");

        var result = await CreateSut().ActivateInviteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PASS_MISMATCH");
    }

    [Fact]
    public async Task ActivateInvite_InviteKhongHopLe_TraVeLoi()
    {
        Repo(i => i.InviteLinks).SetupFind(); // không tìm thấy invite
        var request = new ActivateInviteRequest(Guid.NewGuid(), "Nguyễn Văn C", "0900000000", "matkhau1", "matkhau1");

        var result = await CreateSut().ActivateInviteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVITE_INVALID");
    }

    [Fact]
    public async Task ActivateInvite_VuotQuotaGoi_TraVeQuotaExceeded()
    {
        var invite = Fake.Invite(targetRole: Fake.Role(id: 2, code: "TEACHER"));
        Repo(i => i.InviteLinks).SetupFind(invite);
        _quota.Setup(q => q.CanAddUserAsync(invite.SchoolId, RoleCode.Teacher, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Failure("Hết slot", "QUOTA_EXCEEDED"));

        var request = new ActivateInviteRequest(invite.Token, "Nguyễn Văn C", "0900000000", "matkhau1", "matkhau1");
        var result = await CreateSut().ActivateInviteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task ActivateInvite_HopLe_TaoUserVaTraVeDangNhap()
    {
        // Invite không gắn email cụ thể → bỏ qua bước kiểm tra trùng email, tập trung vào tạo user.
        var invite = Fake.Invite(targetEmail: null, targetRole: Fake.Role(id: 2, code: "TEACHER"));
        Repo(i => i.InviteLinks).SetupFind(invite);
        Repo(u => u.Users).SetupFind();                 // không có admin/không trùng email
        var addedUsers = Repo(u => u.Users).CaptureAdds();
        _quota.Setup(q => q.CanAddUserAsync(It.IsAny<int>(), It.IsAny<RoleCode>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Success(true));
        _hasher.Setup(h => h.HashPassword(It.IsAny<User>(), "matkhau1")).Returns("HASHED");

        var request = new ActivateInviteRequest(invite.Token, "Nguyễn Văn C", "0900000000", "matkhau1", "matkhau1");
        var result = await CreateSut().ActivateInviteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Nguyễn Văn C");
        addedUsers.Should().ContainSingle();
        addedUsers[0].PasswordHash.Should().Be("HASHED");
        invite.UsedAt.Should().NotBeNull();          // invite được đánh dấu đã dùng
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  7. Khôi phục mật khẩu qua số điện thoại
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FindUserByPhone_SoQuaNgan_TraVeLoi()
    {
        var result = await CreateSut().FindUserByPhoneAsync("12345"); // < 9 chữ số

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PHONE");
    }

    [Fact]
    public async Task FindUserByPhone_TonTai_TraVeSoDaCheGiau()
    {
        var user = Fake.User(phone: "0912345678");
        Repo(u => u.Users).SetupFind(user);

        var result = await CreateSut().FindUserByPhoneAsync("0912345678");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().Contain("****");            // đã che bớt để bảo mật
        result.Data.Should().NotBe("0912345678");
    }

    [Fact]
    public async Task ResetPasswordByPhone_KhongTonTai_TraVeNotFound()
    {
        Repo(u => u.Users).SetupFind();

        var result = await CreateSut().ResetPasswordByPhoneAsync("0912345678", "matkhaumoi");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task ResetPasswordByPhone_TonTai_DoiHashVaLuu()
    {
        var user = Fake.User(phone: "0912345678", passwordHash: "OLD");
        Repo(u => u.Users).SetupFind(user);
        _hasher.Setup(h => h.HashPassword(user, "matkhaumoi")).Returns("NEW_HASH");

        var result = await CreateSut().ResetPasswordByPhoneAsync("0912345678", "matkhaumoi");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("NEW_HASH");
        ShouldHaveSaved();
    }
}
