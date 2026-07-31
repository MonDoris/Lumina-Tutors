using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Subscription;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="QuotaService"/> — kiểm tra giới hạn tài khoản/lớp theo gói.
///
/// Quy tắc cốt lõi: quota hiệu lực = quota gốc của gói + Σ add-on đang hiệu lực.
/// Gói -1 nghĩa là KHÔNG giới hạn. Trường không có gói active ⇒ chặn tạo mới.
///
/// Ghi chú: <c>GetQuotaStatusAsync</c> dùng truy vấn nhóm (GroupBy…ToDictionaryAsync)
/// đòi hỏi EF provider thật ⇒ được kiểm bằng Integration test, không nằm ở đây.
/// </summary>
public class QuotaServiceTests : ServiceTestBase
{
    private QuotaService CreateSut() => new(Uow.Object, NullLogger<QuotaService>.Instance);

    /// <summary>Nạp sẵn một đăng ký cho trường (mặc định active) vào repo SchoolSubscriptions.</summary>
    private void GivenSubscription(SchoolSubscription? sub) =>
        Repo(u => u.SchoolSubscriptions).SetupFindOne(sub);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. CanAddUserAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CanAddUser_KhongCoGoiActive_TraVeNoActivePlan()
    {
        GivenSubscription(null);

        var result = await CreateSut().CanAddUserAsync(schoolId: 1, RoleCode.Teacher);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_ACTIVE_PLAN");
    }

    [Fact]
    public async Task CanAddUser_GoiDaHetHan_CoiNhuKhongCoGoi()
    {
        var expired = Fake.Subscription(currentPeriodEnd: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        GivenSubscription(expired);

        var result = await CreateSut().CanAddUserAsync(1, RoleCode.Teacher);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_ACTIVE_PLAN");
    }

    [Fact]
    public async Task CanAddUser_QuotaKhongGioiHan_LuonChoPhep()
    {
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxTeachers: -1)));

        var result = await CreateSut().CanAddUserAsync(1, RoleCode.Teacher);

        result.IsSuccess.Should().BeTrue();
        // Không giới hạn ⇒ không cần đếm số đang dùng
        Repo(u => u.Users).Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>?>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CanAddUser_ConSlot_ChoPhep()
    {
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxTeachers: 5)));
        Repo(u => u.Users).SetupCount(4); // đang dùng 4/5

        var result = await CreateSut().CanAddUserAsync(1, RoleCode.Teacher);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAddUser_DayQuota_TraVeQuotaExceeded()
    {
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxTeachers: 5)));
        Repo(u => u.Users).SetupCount(5); // đang dùng 5/5

        var result = await CreateSut().CanAddUserAsync(1, RoleCode.Teacher);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task CanAddUser_CongThemAddOn_NoiRongGioiHan()
    {
        // Gói gốc cho 5 giáo viên + add-on +3 = 8. Đang dùng 6 ⇒ vẫn còn chỗ.
        var addon = Fake.QuotaAddOn(targetRole: RoleCode.Teacher, extraQuota: 3);
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxTeachers: 5), quotaAddOns: addon));
        Repo(u => u.Users).SetupCount(6);

        var result = await CreateSut().CanAddUserAsync(1, RoleCode.Teacher);

        result.IsSuccess.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. CanAddClassAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CanAddClass_KhongCoGoiActive_TraVeNoActivePlan()
    {
        GivenSubscription(null);

        var result = await CreateSut().CanAddClassAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_ACTIVE_PLAN");
    }

    [Fact]
    public async Task CanAddClass_DayQuota_TraVeQuotaExceeded()
    {
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxClasses: 10)));
        Repo(c => c.Classes).SetupCount(10);

        var result = await CreateSut().CanAddClassAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("QUOTA_EXCEEDED");
    }

    [Fact]
    public async Task CanAddClass_ConSlot_ChoPhep()
    {
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(maxClasses: 10)));
        Repo(c => c.Classes).SetupCount(3);

        var result = await CreateSut().CanAddClassAsync(1);

        result.IsSuccess.Should().BeTrue();
    }
}
