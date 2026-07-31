using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Attendance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="AttendanceService"/> — mở/đóng phiên điểm danh,
/// quét QR (phía học sinh), giáo viên chỉnh tay và thông báo phụ huynh vắng.
/// </summary>
public class AttendanceServiceTests : ServiceTestBase
{
    private AttendanceService CreateSut() => new(
        Uow.Object, Mapper, new Mock<IConfiguration>().Object, NullLogger<AttendanceService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateSession / CloseSession
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateSession_DaCoPhien_TraVeSessionExists()
    {
        Repo(s => s.AttendanceSessions).SetupFind(Fake.Session());

        var result = await CreateSut().CreateSessionAsync(1, 50, new CreateSessionRequest(1, DateOnly.FromDateTime(DateTime.UtcNow)));

        ShouldFail(result, "SESSION_EXISTS");
    }

    [Fact]
    public async Task CloseSession_KhongTonTai_TraVeNotFound()
    {
        Repo(s => s.AttendanceSessions).SetupFindOne(null);

        var result = await CreateSut().CloseSessionAsync(1, 50);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task CloseSession_DaDong_TraVeAlreadyClosed()
    {
        Repo(s => s.AttendanceSessions).SetupFindOne(Fake.Session(status: SessionStatus.Closed));

        var result = await CreateSut().CloseSessionAsync(1, 50);

        ShouldFail(result, "ALREADY_CLOSED");
    }

    [Fact]
    public async Task CloseSession_DangMo_DongVaLuu()
    {
        var session = Fake.Session(status: SessionStatus.Open);
        Repo(s => s.AttendanceSessions).SetupFindOne(session);

        var result = await CreateSut().CloseSessionAsync(session.Id, 50);

        result.IsSuccess.Should().BeTrue();
        session.SessionStatus.Should().Be(SessionStatus.Closed);
        session.ClosedAt.Should().NotBeNull();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. ScanQR (phía học sinh)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ScanQR_MaKhongHopLe_TraVeQrInvalid()
    {
        Repo(s => s.AttendanceSessions).SetupFind(); // không tìm thấy phiên đang mở

        var result = await CreateSut().ScanQRAsync(new ScanQRRequest(Guid.NewGuid(), 100));

        ShouldFail(result, "QR_INVALID");
    }

    [Fact]
    public async Task ScanQR_HetHan_TraVeQrExpired()
    {
        Repo(s => s.AttendanceSessions).SetupFind(
            Fake.Session(qrExpiresAt: DateTime.UtcNow.AddMinutes(-1)));

        var result = await CreateSut().ScanQRAsync(new ScanQRRequest(Guid.NewGuid(), 100));

        ShouldFail(result, "QR_EXPIRED");
    }

    [Fact]
    public async Task ScanQR_KhongThuocLop_TraVeNotEnrolled()
    {
        Repo(s => s.AttendanceSessions).SetupFind(Fake.Session());
        Repo(a => a.StudentAttendances).SetupFindOne(null);

        var result = await CreateSut().ScanQRAsync(new ScanQRRequest(Guid.NewGuid(), 100));

        ShouldFail(result, "NOT_ENROLLED");
    }

    [Fact]
    public async Task ScanQR_DaDiemDanh_KhongGhiDeLai()
    {
        Repo(s => s.AttendanceSessions).SetupFind(Fake.Session());
        Repo(a => a.StudentAttendances).SetupFindOne(
            Fake.Attendance(studentId: 100, status: AttendanceStatus.Present));

        var result = await CreateSut().ScanQRAsync(new ScanQRRequest(Guid.NewGuid(), 100));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Message.Should().Contain("trước đó");
        ShouldNotHaveSaved();
    }

    [Fact]
    public async Task ScanQR_HopLe_GhiNhanCoMatVaLuu()
    {
        Repo(s => s.AttendanceSessions).SetupFind(Fake.Session());
        var record = Fake.Attendance(studentId: 100, status: AttendanceStatus.Absent);
        Repo(a => a.StudentAttendances).SetupFindOne(record);

        var result = await CreateSut().ScanQRAsync(new ScanQRRequest(Guid.NewGuid(), 100));

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Present);
        record.CheckMethod.Should().Be(CheckMethod.QrScan);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. UpdateAttendance (giáo viên chỉnh tay)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAttendance_PhienKhongThuocGiaoVien_TraVeNotFound()
    {
        Repo(s => s.AttendanceSessions).SetupFind(); // không có phiên của giáo viên này

        var result = await CreateSut().UpdateAttendanceAsync(1, 50,
            new UpdateAttendanceRequest(100, AttendanceStatus.Present, null));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAttendance_HopLe_CapNhatTrangThai()
    {
        Repo(s => s.AttendanceSessions).SetupFind(Fake.Session());
        var record = Fake.Attendance(studentId: 100, status: AttendanceStatus.Absent);
        Repo(a => a.StudentAttendances).SetupFindOne(record);

        var result = await CreateSut().UpdateAttendanceAsync(1, 50,
            new UpdateAttendanceRequest(100, AttendanceStatus.Late, "Đi trễ 5 phút"));

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Late);
        record.CheckMethod.Should().Be(CheckMethod.Manual);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. GetSession / GetSessionRecords / NotifyAbsentParents
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSession_KhongTonTai_TraVeNotFound()
    {
        Repo(s => s.AttendanceSessions).SetupFind();

        var result = await CreateSut().GetSessionAsync(1);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetSessionRecords_TraVeDanhSach()
    {
        Repo(a => a.StudentAttendances).SetupFind(
            Fake.Attendance(id: 1, studentId: 100),
            Fake.Attendance(id: 2, studentId: 101));

        var result = await CreateSut().GetSessionRecordsAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task NotifyAbsentParents_DanhDauDaThongBao_TraVeSoLuong()
    {
        var absent = new[]
        {
            Fake.Attendance(id: 1, studentId: 100, status: AttendanceStatus.Absent),
            Fake.Attendance(id: 2, studentId: 101, status: AttendanceStatus.Absent)
        };
        Repo(a => a.StudentAttendances).SetupFind(absent);

        var result = await CreateSut().NotifyAbsentParentsAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
        absent.Should().OnlyContain(r => r.NotifiedParent);
        ShouldHaveSaved();
    }
}
