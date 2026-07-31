using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Attendance;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="LeaveRequestService"/> — phụ huynh gửi đơn xin nghỉ,
/// nhà trường/giáo viên duyệt (kèm ràng buộc ngày và trạng thái đơn).
/// </summary>
public class LeaveRequestServiceTests : ServiceTestBase
{
    private LeaveRequestService CreateSut() => new(Uow.Object, NullLogger<LeaveRequestService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CreateAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_NgayKhongHopLe_TraVeInvalidDates()
    {
        var req = new CreateLeaveRequestRequest(100,
            FromDate: new DateOnly(2026, 1, 10), ToDate: new DateOnly(2026, 1, 5), Reason: "Về quê");

        var result = await CreateSut().CreateAsync(1, parentId: 9, req);

        ShouldFail(result, "INVALID_DATES");
    }

    [Fact]
    public async Task Create_HocSinhKhongThuocTruong_TraVeStudentNotFound()
    {
        Repo(u => u.Users).SetupFind(); // không tìm thấy học sinh trong trường

        var req = new CreateLeaveRequestRequest(100,
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 7), "Về quê");
        var result = await CreateSut().CreateAsync(1, 9, req);

        ShouldFail(result, "STUDENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_HopLe_TaoDonVaTinhSoNgay()
    {
        Repo(u => u.Users).SetupFind(Fake.User(id: 100));
        Repo(sp => sp.StudentProfiles).SetupFind(Fake.StudentProfile(userId: 100, code: "HS0001"));
        Repo(e => e.ClassEnrollments).SetupFind(Fake.Enrollment(studentId: 100));
        var added = Repo(l => l.LeaveRequests).CaptureAdds();

        var req = new CreateLeaveRequestRequest(100,
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 7), "Về quê ăn cưới");
        var result = await CreateSut().CreateAsync(1, 9, req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.DayCount.Should().Be(3);     // 5→7 tháng 1 = 3 ngày
        added.Should().ContainSingle();
        added[0].Status.Should().Be(LeaveRequestStatus.Pending);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. ReviewAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Review_DonKhongTonTai_TraVeNotFound()
    {
        Repo(l => l.LeaveRequests).SetupFindOne(null);

        var result = await CreateSut().ReviewAsync(1, 9, new ReviewLeaveRequestRequest(1, true, null));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task Review_DaXuLy_TraVeAlreadyReviewed()
    {
        Repo(l => l.LeaveRequests).SetupFindOne(new LeaveRequest { Id = 1, Status = LeaveRequestStatus.Approved });

        var result = await CreateSut().ReviewAsync(1, 9, new ReviewLeaveRequestRequest(1, true, null));

        ShouldFail(result, "ALREADY_REVIEWED");
    }

    [Fact]
    public async Task Review_Duyet_ChuyenTrangThaiApproved()
    {
        var entity = new LeaveRequest { Id = 1, Status = LeaveRequestStatus.Pending };
        Repo(l => l.LeaveRequests).SetupFindOne(entity);

        var result = await CreateSut().ReviewAsync(1, reviewerUserId: 9, new ReviewLeaveRequestRequest(1, Approved: true, "Đồng ý"));

        result.IsSuccess.Should().BeTrue();
        entity.Status.Should().Be(LeaveRequestStatus.Approved);
        entity.ReviewedByUserId.Should().Be(9);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task Review_TuChoi_ChuyenTrangThaiRejected()
    {
        var entity = new LeaveRequest { Id = 1, Status = LeaveRequestStatus.Pending };
        Repo(l => l.LeaveRequests).SetupFindOne(entity);

        var result = await CreateSut().ReviewAsync(1, 9, new ReviewLeaveRequestRequest(1, Approved: false, "Không hợp lý"));

        result.IsSuccess.Should().BeTrue();
        entity.Status.Should().Be(LeaveRequestStatus.Rejected);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. GetByParent
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByParent_TraVeDanhSachVaDemChoDuyet()
    {
        Repo(l => l.LeaveRequests).SetupFind(
            new LeaveRequest { Id = 1, StudentId = 100, Status = LeaveRequestStatus.Pending,
                FromDate = new DateOnly(2026,1,5), ToDate = new DateOnly(2026,1,6), Student = Fake.User(id: 100) },
            new LeaveRequest { Id = 2, StudentId = 100, Status = LeaveRequestStatus.Approved,
                FromDate = new DateOnly(2026,1,1), ToDate = new DateOnly(2026,1,2), Student = Fake.User(id: 100) });
        Repo(sp => sp.StudentProfiles).SetupFind(Fake.StudentProfile(userId: 100));
        Repo(e => e.ClassEnrollments).SetupFind(Fake.Enrollment(studentId: 100));

        var result = await CreateSut().GetByParentAsync(parentId: 9);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.PendingCount.Should().Be(1);
    }
}
