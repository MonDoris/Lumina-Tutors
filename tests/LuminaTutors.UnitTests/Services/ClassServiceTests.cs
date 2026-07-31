using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Attendance;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="ClassService"/> — tạo/sửa/xóa lớp, phân công môn,
/// xếp lớp cho học sinh, kiểm tra trùng lịch dạy và cấu hình năm học / khối lớp.
/// </summary>
public class ClassServiceTests : ServiceTestBase
{
    private readonly Mock<IQuotaService> _quota = new();

    private ClassService CreateSut() => new(
        Uow.Object, Mapper, _quota.Object, NullLogger<ClassService>.Instance);

    private void AllowClassQuota() =>
        _quota.Setup(q => q.CanAddClassAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Success(true));

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_VuotGioiHanLop_TraVeLoiQuota()
    {
        _quota.Setup(q => q.CanAddClassAsync(1, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result<bool>.Failure("Hết slot lớp", "QUOTA_EXCEEDED"));

        var result = await CreateSut().CreateAsync(1, new CreateClassRequest("10A1", 1, 1, null));

        ShouldFail(result, "QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task Create_TrungTenLop_TraVeDuplicate()
    {
        AllowClassQuota();
        Repo(c => c.Classes).SetupFindNoInclude(Fake.Class(name: "10A1")); // đã tồn tại

        var result = await CreateSut().CreateAsync(1, new CreateClassRequest("10A1", 1, 1, null));

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task Create_HopLe_LuuLopVaCatKhoangTrangTen()
    {
        AllowClassQuota();
        Repo(c => c.Classes).SetupFindNoInclude();               // không trùng
        Repo(c => c.Classes).SetupFindWithInclude(Fake.Class()); // GetById nạp lại sau khi tạo
        var added = Repo(c => c.Classes).CaptureAdds();

        var result = await CreateSut().CreateAsync(1, new CreateClassRequest("  10A1  ", 2, 3, null, MaxStudents: 45));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].ClassName.Should().Be("10A1");   // đã Trim()
        added[0].MaxStudents.Should().Be(45);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. GetById / Update / Delete
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_KhongTonTai_TraVeNotFound()
    {
        Repo(c => c.Classes).SetupFindWithInclude(); // rỗng

        var result = await CreateSut().GetByIdAsync(1, 99);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetById_TonTai_TraVeChiTiet()
    {
        Repo(c => c.Classes).SetupFindWithInclude(Fake.Class(id: 5, name: "12A3"));

        var result = await CreateSut().GetByIdAsync(1, 5);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ClassId.Should().Be(5);
        result.Data.ClassName.Should().Be("12A3");
    }

    [Fact]
    public async Task Update_KhongTonTai_TraVeNotFound()
    {
        Repo(c => c.Classes).SetupFindOne(null);

        var result = await CreateSut().UpdateAsync(1, 99, new UpdateClassRequest("10A1", null, 40, null));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Update_HopLe_CapNhatTruongVaLuu()
    {
        var cls = Fake.Class(id: 5, name: "10A1");
        Repo(c => c.Classes).SetupFindOne(cls);                    // bản ghi tracked để sửa
        Repo(c => c.Classes).SetupFindWithInclude(cls);           // GetById sau cập nhật

        var result = await CreateSut().UpdateAsync(1, 5, new UpdateClassRequest("10A2", 7, 50, "P.201"));

        result.IsSuccess.Should().BeTrue();
        cls.ClassName.Should().Be("10A2");
        cls.MaxStudents.Should().Be(50);
        cls.HomeRoomTeacherId.Should().Be(7);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Delete_ConHocSinhDangHoc_KhongChoXoa()
    {
        var cls = Fake.Class();
        cls.Enrollments = new List<ClassEnrollment> { Fake.Enrollment(status: EnrollmentStatus.Active) };
        Repo(c => c.Classes).SetupFindWithInclude(cls);

        var result = await CreateSut().DeleteAsync(1, cls.Id);

        ShouldFail(result, "HAS_STUDENTS");
    }

    [Fact]
    public async Task Delete_LopTrong_XoaVaLuu()
    {
        Repo(c => c.Classes).SetupFindWithInclude(Fake.Class()); // Enrollments rỗng

        var result = await CreateSut().DeleteAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        Repo(c => c.Classes).Verify(r => r.Remove(It.IsAny<Class>()), Times.Once());
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. AssignSubject
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AssignSubject_LopKhongTonTai_TraVeNotFound()
    {
        Repo(c => c.Classes).SetupGetById(null);

        var result = await CreateSut().AssignSubjectAsync(1, 1, new AssignSubjectRequest(1, 2, 1));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task AssignSubject_TrungPhanCong_TraVeDuplicate()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(sa => sa.SubjectAssignments).SetupFind(new SubjectAssignment());

        var result = await CreateSut().AssignSubjectAsync(1, 1, new AssignSubjectRequest(1, 2, 1));

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task AssignSubject_HopLe_LuuPhanCong()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(sa => sa.SubjectAssignments).SetupFind();          // chưa phân công
        var added = Repo(sa => sa.SubjectAssignments).CaptureAdds();

        var result = await CreateSut().AssignSubjectAsync(1, 1, new AssignSubjectRequest(SubjectId: 3, TeacherId: 4, SemesterId: 1, PeriodsPerWeek: 3));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].SubjectId.Should().Be(3);
        added[0].PeriodsPerWeek.Should().Be(3);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. Enroll / Remove student
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EnrollStudent_LopKhongTonTai_TraVeNotFound()
    {
        Repo(c => c.Classes).SetupGetById(null);

        var result = await CreateSut().EnrollStudentAsync(1, 1, 100);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task EnrollStudent_KhongPhaiHocSinh_TraVeNotFound()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(u => u.Users).SetupFind(Fake.User(role: Fake.Role(id: 2, code: "TEACHER")));

        var result = await CreateSut().EnrollStudentAsync(1, 1, 100);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task EnrollStudent_DaCoTrongLop_TraVeAlreadyEnrolled()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(u => u.Users).SetupFind(Fake.User(id: 100, role: Fake.Role(id: 3, code: "STUDENT")));
        Repo(e => e.ClassEnrollments).SetupFind(Fake.Enrollment(classId: 1, studentId: 100));

        var result = await CreateSut().EnrollStudentAsync(1, 1, 100);

        ShouldFail(result, "ALREADY_ENROLLED");
    }

    [Fact]
    public async Task EnrollStudent_HopLe_TaoGhiDanhVaLuu()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(u => u.Users).SetupFind(Fake.User(id: 100, role: Fake.Role(id: 3, code: "STUDENT")));
        Repo(e => e.ClassEnrollments).SetupFind();  // chưa ghi danh ở đâu cả
        var added = Repo(e => e.ClassEnrollments).CaptureAdds();

        var result = await CreateSut().EnrollStudentAsync(1, 1, 100);

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].Status.Should().Be(EnrollmentStatus.Active);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task RemoveStudent_KhongCoTrongLop_TraVeNotEnrolled()
    {
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(e => e.ClassEnrollments).SetupFind();

        var result = await CreateSut().RemoveStudentAsync(1, 1, 100);

        ShouldFail(result, "NOT_ENROLLED");
    }

    [Fact]
    public async Task RemoveStudent_HopLe_ChuyenTrangThaiWithdrawn()
    {
        var enrollment = Fake.Enrollment(classId: 1, studentId: 100, status: EnrollmentStatus.Active);
        Repo(c => c.Classes).SetupGetById(Fake.Class());
        Repo(e => e.ClassEnrollments).SetupFind(enrollment);

        var result = await CreateSut().RemoveStudentAsync(1, 1, 100);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().Be(EnrollmentStatus.Withdrawn);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. Cấu hình năm học / khối lớp & kiểm tra trùng lịch
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAcademicYear_NgayKhongHopLe_TraVeInvalidDates()
    {
        var req = new CreateAcademicYearRequest("2025-2026",
            StartDate: new DateOnly(2026, 5, 1), EndDate: new DateOnly(2025, 9, 1));

        var result = await CreateSut().CreateAcademicYearAsync(1, req);

        ShouldFail(result, "INVALID_DATES");
    }

    [Fact]
    public async Task CreateAcademicYear_Trung_TraVeDuplicate()
    {
        Repo(ay => ay.AcademicYears).SetupFind(new AcademicYear());
        var req = new CreateAcademicYearRequest("2025-2026",
            new DateOnly(2025, 9, 1), new DateOnly(2026, 5, 31));

        var result = await CreateSut().CreateAcademicYearAsync(1, req);

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task CreateGradeLevel_BacHocKhongHopLe_TraVeInvalidLevel()
    {
        var req = new CreateGradeLevelRequest(10, "Khối 10", "KHONG_TON_TAI");

        var result = await CreateSut().CreateGradeLevelAsync(1, req);

        ShouldFail(result, "INVALID_LEVEL");
    }

    [Fact]
    public async Task HasScheduleConflict_CoTrungGio_TraVeTrue()
    {
        Repo(s => s.Schedules).SetupFind(new Schedule());

        var conflict = await CreateSut().HasScheduleConflictAsync(1, 1, 5, dayOfWeek: 2, periodStart: 1, periodEnd: 2);

        conflict.Should().BeTrue();
    }

    [Fact]
    public async Task HasScheduleConflict_KhongTrung_TraVeFalse()
    {
        Repo(s => s.Schedules).SetupFind(); // không có lịch trùng

        var conflict = await CreateSut().HasScheduleConflictAsync(1, 1, 5, dayOfWeek: 2, periodStart: 1, periodEnd: 2);

        conflict.Should().BeFalse();
    }
}
