using LuminaTutors.Application.DTOs.Student;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="StudentService"/> — tìm kiếm, tạo/sửa/ngừng hoạt động học sinh,
/// xếp lớp, chuyển lớp và truy vấn theo lớp / theo phụ huynh.
/// </summary>
public class StudentServiceTests : ServiceTestBase
{
    private readonly Mock<IPasswordHasher<User>> _hasher = new();
    private readonly Mock<IQuotaService>          _quota  = new();

    private StudentService CreateSut() => new(
        Uow.Object, Mapper, _hasher.Object, _quota.Object, NullLogger<StudentService>.Instance);

    private void AllowQuota() =>
        _quota.Setup(q => q.CanAddUserAsync(It.IsAny<int>(), RoleCode.Student, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Success(true));

    // ══════════════════════════════════════════════════════════════════════════
    //  1. SearchAsync / GetByIdAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_TraVeDanhSachPhanTrang()
    {
        Repo(sp => sp.StudentProfiles).SetupPaged(
            Fake.StudentProfile(userId: 100, code: "HS0001"),
            Fake.StudentProfile(userId: 101, code: "HS0002"));

        var result = await CreateSut().SearchAsync(1, new StudentSearchRequest(null, null, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_KhongTonTai_TraVeNotFound()
    {
        Repo(sp => sp.StudentProfiles).SetupFindWithInclude();

        var result = await CreateSut().GetByIdAsync(1, 5);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetById_TonTai_TraVeChiTiet()
    {
        Repo(sp => sp.StudentProfiles).SetupFindWithInclude(
            Fake.StudentProfile(userId: 100, code: "HS0001"));

        var result = await CreateSut().GetByIdAsync(1, 100);

        result.IsSuccess.Should().BeTrue();
        result.Data!.StudentCode.Should().Be("HS0001");
        result.Data.Parents.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. CreateAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_VuotQuota_TraVeLoiQuota()
    {
        _quota.Setup(q => q.CanAddUserAsync(1, RoleCode.Student, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Failure("Hết slot học sinh", "QUOTA_EXCEEDED"));

        var result = await CreateSut().CreateAsync(1, NewStudentRequest());

        ShouldFail(result, "QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task Create_EmailTrung_TraVeEmailExists()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind(Fake.User(email: "hs@ds.edu.vn"));

        var result = await CreateSut().CreateAsync(1, NewStudentRequest());

        ShouldFail(result, "EMAIL_EXISTS");
    }

    [Fact]
    public async Task Create_ThieuVaiTro_TraVeConfigError()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind();  // không trùng email
        Repo(r => r.Roles).SetupFind();  // không có role Student

        var result = await CreateSut().CreateAsync(1, NewStudentRequest());

        ShouldFail(result, "CONFIG_ERROR");
    }

    [Fact]
    public async Task Create_HopLe_TaoUserVaProfile()
    {
        AllowQuota();
        Repo(u => u.Users).SetupFind();
        Repo(r => r.Roles).SetupFind(Fake.Role(id: 3, code: "STUDENT", name: "Học sinh"));
        var addedUsers    = Repo(u => u.Users).CaptureAdds();
        var addedProfiles = Repo(sp => sp.StudentProfiles).CaptureAdds();
        Repo(sp => sp.StudentProfiles).SetupFindWithInclude(Fake.StudentProfile(userId: 100)); // GetById cuối
        _hasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("HASH");

        var result = await CreateSut().CreateAsync(1, NewStudentRequest());

        result.IsSuccess.Should().BeTrue();
        addedUsers.Should().ContainSingle();
        addedProfiles.Should().ContainSingle();
        addedProfiles[0].StudentCode.Should().Be("HS0001");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Update / Deactivate
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_KhongTonTai_TraVeNotFound()
    {
        Repo(sp => sp.StudentProfiles).SetupFindOne(null);

        var result = await CreateSut().UpdateAsync(1, 5, new UpdateStudentRequest("A", null, null, null, null, null, null));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Update_HopLe_CapNhatThongTin()
    {
        var profile = Fake.StudentProfile(userId: 100);
        Repo(sp => sp.StudentProfiles).SetupFindOne(profile);
        Repo(sp => sp.StudentProfiles).SetupFindWithInclude(profile); // GetById sau cập nhật

        var result = await CreateSut().UpdateAsync(1, 100,
            new UpdateStudentRequest("Nguyễn Văn Mới", "0912345678", null, Gender.Male, null, null, null));

        result.IsSuccess.Should().BeTrue();
        profile.User.FullName.Should().Be("Nguyễn Văn Mới");
        profile.Gender.Should().Be(Gender.Male);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Deactivate_KhongTonTai_TraVeNotFound()
    {
        Repo(sp => sp.StudentProfiles).SetupFindOne(null);

        var result = await CreateSut().DeactivateAsync(1, 5);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Deactivate_HopLe_TatHoatDongVaRutKhoiLop()
    {
        var profile = Fake.StudentProfile(userId: 100);
        Repo(sp => sp.StudentProfiles).SetupFindOne(profile);
        var enrollment = Fake.Enrollment(studentId: 100, status: EnrollmentStatus.Active);
        Repo(e => e.ClassEnrollments).SetupFind(enrollment);

        var result = await CreateSut().DeactivateAsync(1, 100);

        result.IsSuccess.Should().BeTrue();
        profile.User.IsActive.Should().BeFalse();
        enrollment.Status.Should().Be(EnrollmentStatus.Withdrawn);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. Enroll / Transfer
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_DaCoLopCungNamHoc_TraVeAlreadyEnrolled()
    {
        Repo(sp => sp.StudentProfiles).SetupFindNoInclude(Fake.StudentProfile(userId: 100));
        Repo(c => c.Classes).SetupGetById(Fake.Class(id: 2));
        Repo(e => e.ClassEnrollments).SetupFindWithInclude(Fake.Enrollment(studentId: 100));

        var result = await CreateSut().EnrollAsync(1, 100, new EnrollStudentRequest(2, null));

        ShouldFail(result, "ALREADY_ENROLLED");
    }

    [Fact]
    public async Task Enroll_HopLe_TaoGhiDanh()
    {
        Repo(sp => sp.StudentProfiles).SetupFindNoInclude(Fake.StudentProfile(userId: 100));
        Repo(c => c.Classes).SetupGetById(Fake.Class(id: 2));
        Repo(e => e.ClassEnrollments).SetupFindWithInclude(); // chưa xếp lớp
        var added = Repo(e => e.ClassEnrollments).CaptureAdds();

        var result = await CreateSut().EnrollAsync(1, 100, new EnrollStudentRequest(2, null));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].ClassId.Should().Be(2);
    }

    [Fact]
    public async Task Transfer_KhongCoLopHienTai_TraVeNotFound()
    {
        Repo(sp => sp.StudentProfiles).SetupFindNoInclude(Fake.StudentProfile(userId: 100));
        Repo(e => e.ClassEnrollments).SetupFindNoInclude(); // không có lớp đang học

        var result = await CreateSut().TransferAsync(1, 100, new TransferStudentRequest(3, "Chuyển nhà"));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Transfer_HopLe_DoiTrangThaiVaTaoGhiDanhMoi()
    {
        var current = Fake.Enrollment(classId: 2, studentId: 100, status: EnrollmentStatus.Active);
        Repo(sp => sp.StudentProfiles).SetupFindNoInclude(Fake.StudentProfile(userId: 100));
        Repo(e => e.ClassEnrollments).SetupFindNoInclude(current);
        Repo(c => c.Classes).SetupGetById(Fake.Class(id: 3));
        var added = Repo(e => e.ClassEnrollments).CaptureAdds();

        var result = await CreateSut().TransferAsync(1, 100, new TransferStudentRequest(3, "Chuyển nhà"));

        result.IsSuccess.Should().BeTrue();
        current.Status.Should().Be(EnrollmentStatus.Transferred);
        added.Should().ContainSingle();
        added[0].ClassId.Should().Be(3);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. GetByClass / GetChildrenOfParent
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByClass_TraVeHocSinhTrongLop()
    {
        Repo(e => e.ClassEnrollments).SetupFind(Fake.Enrollment(classId: 5, studentId: 100));
        Repo(sp => sp.StudentProfiles).SetupFindWithInclude(Fake.StudentProfile(userId: 100));

        var result = await CreateSut().GetByClassAsync(5);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
    }

    [Fact]
    public async Task GetChildrenOfParent_KhongCoLienKet_TraVeRong()
    {
        Repo(r => r.ParentStudentRelations).SetupFind(); // không có con

        var result = await CreateSut().GetChildrenOfParentAsync(parentUserId: 9, schoolId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }

    // ─── Helper dựng request tạo học sinh mẫu ─────────────────────────────────
    private static CreateStudentRequest NewStudentRequest() => new(
        FullName: "Nguyễn Văn A",
        Email: "hocsinha",
        StudentCode: "HS0001",
        DateOfBirth: new DateOnly(2010, 5, 1),
        Gender: Gender.Male,
        PlaceOfBirth: null,
        PermanentAddress: null,
        EthnicGroup: null,
        AdmissionDate: null);
}
