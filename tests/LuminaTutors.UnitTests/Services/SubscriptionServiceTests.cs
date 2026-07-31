using LuminaTutors.Application.DTOs.Subscription;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Subscription;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="SubscriptionService"/> — đăng ký/nâng cấp gói, mua add-on,
/// hủy/tự gia hạn, xác nhận thanh toán đơn (idempotent), kiểm tra entitlement (gating)
/// và một số thao tác quản trị E-Selling (SYSADMIN).
/// </summary>
public class SubscriptionServiceTests : ServiceTestBase
{
    private readonly Mock<IPasswordHasher<User>>  _hasher      = new();
    private readonly Mock<IBillingEmailService>   _billingMail = new();

    private SubscriptionService CreateSut()
    {
        _billingMail
            .Setup(m => m.SendSubscriptionReceiptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        return new SubscriptionService(
            Uow.Object, _hasher.Object, _billingMail.Object, NullLogger<SubscriptionService>.Instance);
    }

    private void GivenSubscription(SchoolSubscription? sub) =>
        Repo(s => s.SchoolSubscriptions).SetupFindOne(sub);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. SubscribeOrUpgrade
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Subscribe_GoiKhongTonTai_TraVePlanNotFound()
    {
        Repo(p => p.SubscriptionPlans).SetupGetById(null);

        var result = await CreateSut().SubscribeOrUpgradeAsync(1, 9, new ChangePlanRequest(5));

        ShouldFail(result, "PLAN_NOT_FOUND");
    }

    [Fact]
    public async Task Subscribe_DangDungGoiNay_TraVeSamePlan()
    {
        Repo(p => p.SubscriptionPlans).SetupGetById(Fake.Plan(id: 1));
        GivenSubscription(Fake.Subscription(plan: Fake.Plan(id: 1))); // đang active, cùng PlanId

        var result = await CreateSut().SubscribeOrUpgradeAsync(1, 9, new ChangePlanRequest(1));

        ShouldFail(result, "SAME_PLAN");
    }

    [Fact]
    public async Task Subscribe_KhongPhaiNangCap_TraVeNotUpgrade()
    {
        var currentPlan = Fake.Plan(id: 1); currentPlan.Tier = 2;
        var targetPlan  = Fake.Plan(id: 2); targetPlan.Tier  = 1;   // tier thấp hơn
        Repo(p => p.SubscriptionPlans).SetupGetById(targetPlan);
        GivenSubscription(Fake.Subscription(plan: currentPlan));

        var result = await CreateSut().SubscribeOrUpgradeAsync(1, 9, new ChangePlanRequest(2));

        ShouldFail(result, "NOT_UPGRADE");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. BuyAddOn / BuyQuotaAddOn
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BuyAddOn_KhongTonTai_TraVeAddonNotFound()
    {
        Repo(a => a.SubscriptionAddOns).SetupGetById(null);

        var result = await CreateSut().BuyAddOnAsync(1, 9, addOnId: 5);

        ShouldFail(result, "ADDON_NOT_FOUND");
    }

    [Fact]
    public async Task BuyAddOn_KhongCoGoiActive_TraVeNoActivePlan()
    {
        Repo(a => a.SubscriptionAddOns).SetupGetById(new SubscriptionAddOn { Id = 1, IsActive = true, Feature = PremiumFeature.AiTutor });
        GivenSubscription(null);

        var result = await CreateSut().BuyAddOnAsync(1, 9, 1);

        ShouldFail(result, "NO_ACTIVE_PLAN");
    }

    [Fact]
    public async Task BuyAddOn_GoiDaBaoGom_TraVeAlreadyInPlan()
    {
        Repo(a => a.SubscriptionAddOns).SetupGetById(new SubscriptionAddOn { Id = 1, IsActive = true, Feature = PremiumFeature.AiTutor });
        var sub = Fake.Subscription();
        sub.Plan.IncludesAiTutor = true;
        GivenSubscription(sub);

        var result = await CreateSut().BuyAddOnAsync(1, 9, 1);

        ShouldFail(result, "ALREADY_IN_PLAN");
    }

    [Fact]
    public async Task BuyQuotaAddOn_KhongTonTai_TraVeQuotaAddonNotFound()
    {
        Repo(a => a.RoleQuotaAddOns).SetupGetById(null);

        var result = await CreateSut().BuyQuotaAddOnAsync(1, 9, quotaAddOnId: 5);

        ShouldFail(result, "QUOTA_ADDON_NOT_FOUND");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Cancel / SetAutoRenew
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cancel_ChuaCoDangKy_TraVeNoSubscription()
    {
        GivenSubscription(null);

        var result = await CreateSut().CancelAsync(1);

        ShouldFail(result, "NO_SUBSCRIPTION");
    }

    [Fact]
    public async Task Cancel_HopLe_TatTuGiaHan()
    {
        var sub = Fake.Subscription();
        GivenSubscription(sub);

        var result = await CreateSut().CancelAsync(1);

        result.IsSuccess.Should().BeTrue();
        sub.AutoRenew.Should().BeFalse();
        sub.CancelledAt.Should().NotBeNull();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task SetAutoRenew_HopLe_CapNhatCo()
    {
        var sub = Fake.Subscription();
        sub.AutoRenew = false;
        GivenSubscription(sub);

        var result = await CreateSut().SetAutoRenewAsync(1, enabled: true);

        result.IsSuccess.Should().BeTrue();
        sub.AutoRenew.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. ConfirmOrderPayment (idempotent)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConfirmOrder_KhongTonTai_TraVeNotFound()
    {
        Repo(o => o.SubscriptionOrders).SetupFindOne(null);

        var result = await CreateSut().ConfirmOrderPaymentAsync(1, 500_000, PaymentMethod.VnPay, "TXN", null);

        result.Should().Be(SubscriptionConfirmResult.NotFound);
    }

    [Fact]
    public async Task ConfirmOrder_DaThanhToan_TraVeAlreadyConfirmed()
    {
        Repo(o => o.SubscriptionOrders).SetupFindOne(new SubscriptionOrder { Id = 1, Status = SubscriptionOrderStatus.Paid, Amount = 500_000 });

        var result = await CreateSut().ConfirmOrderPaymentAsync(1, 500_000, PaymentMethod.VnPay, "TXN", null);

        result.Should().Be(SubscriptionConfirmResult.AlreadyConfirmed);
    }

    [Fact]
    public async Task ConfirmOrder_SaiSoTien_TraVeInvalidAmount()
    {
        Repo(o => o.SubscriptionOrders).SetupFindOne(new SubscriptionOrder { Id = 1, Status = SubscriptionOrderStatus.Pending, Amount = 500_000 });

        var result = await CreateSut().ConfirmOrderPaymentAsync(1, 400_000, PaymentMethod.VnPay, "TXN", null);

        result.Should().Be(SubscriptionConfirmResult.InvalidAmount);
    }

    [Fact]
    public async Task ConfirmOrder_DungSoTien_KichHoatDangKy()
    {
        var sub   = Fake.Subscription(status: SubscriptionStatus.PendingPayment);
        var order = new SubscriptionOrder
        {
            Id           = 1,
            Status       = SubscriptionOrderStatus.Pending,
            Amount       = 500_000,
            OrderType    = SubscriptionOrderType.New,
            PlanId       = 1,
            BillingCycle = SubscriptionCycle.Monthly,
            PeriodStart  = DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEnd    = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Subscription = sub
        };
        Repo(o => o.SubscriptionOrders).SetupFindOne(order);

        var result = await CreateSut().ConfirmOrderPaymentAsync(1, 500_000, PaymentMethod.VnPay, "TXN123", "{raw}");

        result.Should().Be(SubscriptionConfirmResult.Ok);
        order.Status.Should().Be(SubscriptionOrderStatus.Paid);
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task ConfirmOrder_ThanhCong_GuiHoaDonVeNhaTruong()
    {
        var order = new SubscriptionOrder
        {
            Id           = 7,
            Status       = SubscriptionOrderStatus.Pending,
            Amount       = 500_000,
            OrderType    = SubscriptionOrderType.New,
            PlanId       = 1,
            BillingCycle = SubscriptionCycle.Monthly,
            PeriodStart  = DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEnd    = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Subscription = Fake.Subscription(status: SubscriptionStatus.PendingPayment)
        };
        Repo(o => o.SubscriptionOrders).SetupFindOne(order);

        await CreateSut().ConfirmOrderPaymentAsync(7, 500_000, PaymentMethod.VnPay, "TXN123", null);

        _billingMail.Verify(m => m.SendSubscriptionReceiptAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrder_DaThanhToanTruocDo_KhongGuiLaiHoaDon()
    {
        Repo(o => o.SubscriptionOrders).SetupFindOne(
            new SubscriptionOrder { Id = 7, Status = SubscriptionOrderStatus.Paid, Amount = 500_000 });

        await CreateSut().ConfirmOrderPaymentAsync(7, 500_000, PaymentMethod.VnPay, "TXN", null);

        _billingMail.Verify(m => m.SendSubscriptionReceiptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmOrder_GuiMailLoi_VanKichHoatThanhCong()
    {
        var sub   = Fake.Subscription(status: SubscriptionStatus.PendingPayment);
        var order = new SubscriptionOrder
        {
            Id           = 8,
            Status       = SubscriptionOrderStatus.Pending,
            Amount       = 500_000,
            OrderType    = SubscriptionOrderType.New,
            PlanId       = 1,
            BillingCycle = SubscriptionCycle.Monthly,
            PeriodStart  = DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEnd    = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Subscription = sub
        };
        Repo(o => o.SubscriptionOrders).SetupFindOne(order);

        var sut = CreateSut();
        _billingMail
            .Setup(m => m.SendSubscriptionReceiptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var result = await sut.ConfirmOrderPaymentAsync(8, 500_000, PaymentMethod.VnPay, "TXN", null);

        result.Should().Be(SubscriptionConfirmResult.Ok);
        order.Status.Should().Be(SubscriptionOrderStatus.Paid);
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. Entitlement (gating)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HasFeature_KhongCoGoi_TraVeFalse()
    {
        GivenSubscription(null);

        var has = await CreateSut().HasFeatureAsync(1, PremiumFeature.AiTutor);

        has.Should().BeFalse();
    }

    [Fact]
    public async Task HasFeature_GoiBaoGom_TraVeTrue()
    {
        var sub = Fake.Subscription();
        sub.Plan.IncludesAiTutor = true;
        GivenSubscription(sub);

        var has = await CreateSut().HasFeatureAsync(1, PremiumFeature.AiTutor);

        has.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveSubscription_ConHan_TraVeTrue()
    {
        GivenSubscription(Fake.Subscription(status: SubscriptionStatus.Active));

        var active = await CreateSut().HasActiveSubscriptionAsync(1);

        active.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  6. E-Selling admin (SYSADMIN)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TogglePlanActive_KhongTonTai_TraVeNotFound()
    {
        Repo(p => p.SubscriptionPlans).SetupGetById(null);

        var result = await CreateSut().TogglePlanActiveAsync(1);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task TogglePlanActive_HopLe_DaoTrangThai()
    {
        var plan = Fake.Plan(id: 1); plan.IsActive = true;
        Repo(p => p.SubscriptionPlans).SetupGetById(plan);

        var result = await CreateSut().TogglePlanActiveAsync(1);

        result.IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task SavePlan_TrungMa_TraVeDupCode()
    {
        Repo(p => p.SubscriptionPlans).SetupAny(true); // mã đã tồn tại

        var req = new PlanEditRequest(null, "PREMIUM", "Gói Premium", null, 1, 100000, 270000, 1000000, false, false);
        var result = await CreateSut().SavePlanAsync(req);

        ShouldFail(result, "DUP_CODE");
    }

    [Fact]
    public async Task SaveQuotaAddOn_KhongCoSlot_TraVeEmptyQuota()
    {
        var req = new QuotaAddOnEditRequest(null, "QUOTA_X", "Gói mở rộng", null,
            TargetRole: RoleCode.Teacher, ExtraQuota: 0, ExtraClasses: 0,
            MonthlyPrice: 100000, QuarterlyPrice: 270000, YearlyPrice: 1000000);

        var result = await CreateSut().SaveQuotaAddOnAsync(req);

        ShouldFail(result, "EMPTY_QUOTA");
    }

    [Fact]
    public async Task OnboardSchool_ThieuVaiTroAdmin_TraVeConfig()
    {
        Repo(r => r.Roles).SetupFind(); // không có vai trò ADMIN

        var req = new OnboardSchoolRequest("Trường mới", "Hiệu trưởng", "ht@truong.edu.vn", "MatKhau@123", null);
        var result = await CreateSut().OnboardSchoolAsync(req);

        ShouldFail(result, "CONFIG");
    }

    [Fact]
    public async Task OnboardSchool_HopLe_TaoTruongVaTaiKhoanAdmin()
    {
        Repo(r => r.Roles).SetupFind(Fake.Role(id: 1, code: "ADMIN", name: "Nhà trường"));
        Repo(s => s.Schools).SetupAny(false);              // mã trường chưa dùng
        var addedSchools = Repo(s => s.Schools).CaptureAdds();
        var addedUsers   = Repo(u => u.Users).CaptureAdds();
        _hasher.Setup(h => h.HashPassword(It.IsAny<User>(), "MatKhau@123")).Returns("HASH");

        var req = new OnboardSchoolRequest("Trường THPT Mới", "Hiệu trưởng", "ht@truong.edu.vn", "MatKhau@123", null);
        var result = await CreateSut().OnboardSchoolAsync(req);

        result.IsSuccess.Should().BeTrue();
        addedSchools.Should().ContainSingle();
        addedUsers.Should().ContainSingle();
        addedUsers[0].PasswordHash.Should().Be("HASH");
    }
}
