using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Subscription;

// ─── Catalog ────────────────────────────────────────────────────────────────────

public class SubscriptionPlanDto
{
    public int     PlanId             { get; init; }
    public string  PlanCode           { get; init; } = "";
    public string  Name               { get; init; } = "";
    public string? Description        { get; init; }
    public int     Tier               { get; init; }
    public decimal MonthlyPrice       { get; init; }
    public decimal QuarterlyPrice     { get; init; }
    public decimal YearlyPrice        { get; init; }
    public bool    IncludesAiTutor    { get; init; }
    public bool    IncludesVirtualLab { get; init; }
    public bool    IsCurrent          { get; init; }   // gói trường đang dùng
    public bool    IsUpgrade          { get; init; }   // tier cao hơn gói hiện tại → có thể nâng cấp
}

public class SubscriptionAddOnDto
{
    public int       AddOnId        { get; init; }
    public string    AddOnCode      { get; init; } = "";
    public string    Name           { get; init; } = "";
    public string?   Description    { get; init; }
    public string    Feature        { get; init; } = "";
    public decimal   MonthlyPrice   { get; init; }
    public decimal   QuarterlyPrice { get; init; }
    public decimal   YearlyPrice    { get; init; }
    public bool      IsOwned        { get; init; }   // đã có (qua gói hoặc đã mua)
    public bool      InCurrentPlan  { get; init; }   // gói hiện tại đã bao gồm sẵn
    public DateOnly? ActiveUntil    { get; init; }
}

// ─── Current state ──────────────────────────────────────────────────────────────

public class ActiveAddOnDto
{
    public int      AddOnId     { get; init; }
    public string   Name        { get; init; } = "";
    public string   Feature     { get; init; } = "";
    public DateOnly ActiveUntil { get; init; }
}

public class CurrentSubscriptionDto
{
    public bool      HasSubscription  { get; init; }
    public int       SubscriptionId   { get; init; }
    public string    PlanCode         { get; init; } = "";
    public string    PlanName         { get; init; } = "";
    public int       Tier             { get; init; }
    public string    Status           { get; init; } = "";
    public string    BillingCycle     { get; init; } = "";
    public DateOnly  StartDate        { get; init; }
    public DateOnly  CurrentPeriodEnd { get; init; }
    public bool      AutoRenew        { get; init; }
    public bool      IsActive         { get; init; }   // Active && chưa hết hạn
    public int       DaysRemaining    { get; init; }
    public bool      HasAiTutor       { get; init; }
    public bool      HasVirtualLab    { get; init; }
    public List<ActiveAddOnDto> ActiveAddOns { get; init; } = new();
}

public class SubscriptionOrderItemDto
{
    public string  ItemType    { get; init; } = "";
    public string  Description { get; init; } = "";
    public decimal Amount      { get; init; }
}

public class SubscriptionOrderDto
{
    public int       OrderId      { get; init; }
    public string    OrderCode    { get; init; } = "";
    public string    OrderType    { get; init; } = "";
    public string    Status       { get; init; } = "";
    public string?   PlanName     { get; init; }
    public string    BillingCycle { get; init; } = "";
    public decimal   Amount       { get; init; }
    public DateOnly  PeriodStart  { get; init; }
    public DateOnly  PeriodEnd    { get; init; }
    public string?   PaymentMethod { get; init; }
    public DateTime  CreatedAt    { get; init; }
    public DateTime? PaidAt       { get; init; }
    public List<SubscriptionOrderItemDto> Items { get; init; } = new();
}

/// <summary>Dữ liệu tổng hợp cho trang quản lý gói.</summary>
public class SubscriptionOverviewDto
{
    public CurrentSubscriptionDto    Current      { get; init; } = new();
    public List<SubscriptionPlanDto> Plans        { get; init; } = new();
    public List<SubscriptionAddOnDto> AddOns      { get; init; } = new();
    public List<SubscriptionOrderDto> RecentOrders { get; init; } = new();
    public bool VnPayEnabled { get; set; }
}

// ─── Requests ───────────────────────────────────────────────────────────────────

public record ChangePlanRequest(
    [Required(ErrorMessage = "Vui lòng chọn gói.")] int PlanId,
    SubscriptionCycle Cycle      = SubscriptionCycle.Monthly,
    bool              AutoRenew  = true
);

public record BuyAddOnRequest(
    [Required(ErrorMessage = "Vui lòng chọn tính năng.")] int AddOnId
);
