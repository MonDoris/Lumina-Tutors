using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Subscription;

// ─── SubscriptionPlan (catalog cấp nền tảng — không thuộc trường nào) ─────────────

/// <summary>
/// Gói dịch vụ bán cho trường (Basic / Premium…). Mỗi gói có giá theo 3 chu kỳ
/// và cờ cho biết bao gồm sẵn tính năng cao cấp nào.
/// </summary>
public class SubscriptionPlan : AuditableEntity
{
    public string  PlanCode    { get; set; } = string.Empty;   // BASIC, PREMIUM
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int     Tier        { get; set; }                   // càng cao càng xịn — dùng so sánh nâng cấp

    public decimal MonthlyPrice   { get; set; }
    public decimal QuarterlyPrice { get; set; }
    public decimal YearlyPrice    { get; set; }

    public bool IncludesAiTutor    { get; set; }
    public bool IncludesVirtualLab { get; set; }
    public bool IsActive           { get; set; } = true;

    public ICollection<SchoolSubscription> Subscriptions { get; set; } = new List<SchoolSubscription>();

    /// <summary>Giá tương ứng chu kỳ.</summary>
    public decimal PriceFor(SubscriptionCycle cycle) => cycle switch
    {
        SubscriptionCycle.Monthly   => MonthlyPrice,
        SubscriptionCycle.Quarterly => QuarterlyPrice,
        SubscriptionCycle.Yearly    => YearlyPrice,
        _                           => MonthlyPrice
    };

    public bool Includes(PremiumFeature feature) => feature switch
    {
        PremiumFeature.AiTutor    => IncludesAiTutor,
        PremiumFeature.VirtualLab => IncludesVirtualLab,
        _                         => false
    };
}

// ─── SubscriptionAddOn (catalog tính năng lẻ — mua thêm trên nền gói) ─────────────

/// <summary>Tính năng bán lẻ (AI Tutor, Virtual Lab) mua thêm khi gói chưa bao gồm.</summary>
public class SubscriptionAddOn : AuditableEntity
{
    public string         AddOnCode   { get; set; } = string.Empty;  // AI_TUTOR, VIRTUAL_LAB
    public string         Name        { get; set; } = string.Empty;
    public string?        Description { get; set; }
    public PremiumFeature Feature     { get; set; }                   // tính năng mở khóa

    public decimal MonthlyPrice   { get; set; }
    public decimal QuarterlyPrice { get; set; }
    public decimal YearlyPrice    { get; set; }
    public bool    IsActive       { get; set; } = true;

    public decimal PriceFor(SubscriptionCycle cycle) => cycle switch
    {
        SubscriptionCycle.Monthly   => MonthlyPrice,
        SubscriptionCycle.Quarterly => QuarterlyPrice,
        SubscriptionCycle.Yearly    => YearlyPrice,
        _                           => MonthlyPrice
    };
}

// ─── SchoolSubscription (trạng thái đăng ký của 1 trường) ─────────────────────────

public class SchoolSubscription : TenantEntity
{
    public int                PlanId           { get; set; }
    public SubscriptionCycle  BillingCycle     { get; set; } = SubscriptionCycle.Monthly;
    public SubscriptionStatus Status           { get; set; } = SubscriptionStatus.PendingPayment;
    public DateOnly           StartDate        { get; set; }
    public DateOnly           CurrentPeriodEnd { get; set; }
    public bool               AutoRenew        { get; set; } = true;   // true = đăng ký định kỳ tự gia hạn
    public DateTime?          CancelledAt      { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;
    public ICollection<SchoolSubscriptionAddOn> AddOns { get; set; } = new List<SchoolSubscriptionAddOn>();
    public ICollection<SubscriptionOrder>       Orders { get; set; } = new List<SubscriptionOrder>();
}

// ─── SchoolSubscriptionAddOn (add-on đang gắn vào đăng ký của trường) ─────────────

public class SchoolSubscriptionAddOn : AuditableEntity
{
    public int      SubscriptionId { get; set; }
    public int      AddOnId        { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateOnly ActiveUntil    { get; set; }   // hết hiệu lực, canh theo kỳ của gói

    public SchoolSubscription Subscription { get; set; } = null!;
    public SubscriptionAddOn  AddOn        { get; set; } = null!;
}

// ─── SubscriptionOrder (1 giao dịch thanh toán: đăng ký / nâng cấp / add-on / gia hạn) ─

public class SubscriptionOrder : TenantEntity
{
    public int                     SubscriptionId { get; set; }
    public string                  OrderCode      { get; set; } = string.Empty;
    public SubscriptionOrderType   OrderType      { get; set; }
    public SubscriptionOrderStatus Status         { get; set; } = SubscriptionOrderStatus.Pending;

    public int?              PlanId       { get; set; }       // gói đăng ký/nâng cấp/gia hạn (null cho đơn add-on thuần)
    public SubscriptionCycle BillingCycle { get; set; }
    public decimal           Amount       { get; set; }
    public DateOnly          PeriodStart  { get; set; }
    public DateOnly          PeriodEnd    { get; set; }

    public PaymentMethod? PaymentMethod   { get; set; }
    public string?        TransactionCode { get; set; }
    public string?        GatewayResponse { get; set; }
    public DateTime?      PaidAt          { get; set; }
    public int?           CreatedByUserId { get; set; }
    public string?        Notes           { get; set; }

    public SchoolSubscription Subscription { get; set; } = null!;
    public SubscriptionPlan?  Plan         { get; set; }
    public ICollection<SubscriptionOrderItem> Items { get; set; } = new List<SubscriptionOrderItem>();
}

public class SubscriptionOrderItem : BaseEntity
{
    public int                  OrderId     { get; set; }
    public SubscriptionItemType ItemType    { get; set; }
    public int                  RefId       { get; set; }   // PlanId hoặc AddOnId
    public string               Description { get; set; } = string.Empty;
    public decimal              Amount      { get; set; }

    public SubscriptionOrder Order { get; set; } = null!;
}
