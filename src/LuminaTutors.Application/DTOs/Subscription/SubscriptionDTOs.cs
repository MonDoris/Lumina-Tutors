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
    public bool    IsActive           { get; init; }
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
    public bool      IsActive       { get; init; }
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

// ─── E-Selling admin (SYSADMIN) ───────────────────────────────────────────────

/// <summary>Catalog cho trang quản trị E-Selling: toàn bộ gói & add-on (kể cả ẩn).</summary>
public class CatalogDto
{
    public List<SubscriptionPlanDto>  Plans  { get; init; } = new();
    public List<SubscriptionAddOnDto> AddOns { get; init; } = new();
}

/// <summary>Một dòng trong bảng "đăng ký của các trường".</summary>
public class SchoolSubscriptionRowDto
{
    public bool     HasSubscription  { get; init; }
    public int      SchoolId         { get; init; }
    public string   SchoolName       { get; init; } = "";
    public string   PlanName         { get; init; } = "";
    public string   Status           { get; init; } = "";
    public string   BillingCycle     { get; init; } = "";
    public DateOnly CurrentPeriodEnd { get; init; }
    public bool     AutoRenew        { get; init; }
    public bool     IsActive         { get; init; }
    public string   AddOns           { get; init; } = "";   // tên add-on đang hoạt động, phân tách dấu phẩy
}

public record PlanEditRequest(
    int?    PlanId,
    [Required(ErrorMessage = "Vui lòng nhập mã gói."), MaxLength(30)] string PlanCode,
    [Required(ErrorMessage = "Vui lòng nhập tên gói."), MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    [Range(0, 99)] int Tier,
    [Range(0, 1_000_000_000)] decimal MonthlyPrice,
    [Range(0, 1_000_000_000)] decimal QuarterlyPrice,
    [Range(0, 1_000_000_000)] decimal YearlyPrice,
    bool IncludesAiTutor,
    bool IncludesVirtualLab,
    bool IsActive = true
);

public record AddOnEditRequest(
    int?    AddOnId,
    [Required(ErrorMessage = "Vui lòng nhập mã add-on."), MaxLength(30)] string AddOnCode,
    [Required(ErrorMessage = "Vui lòng nhập tên add-on."), MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    [Required] PremiumFeature Feature,
    [Range(0, 1_000_000_000)] decimal MonthlyPrice,
    [Range(0, 1_000_000_000)] decimal QuarterlyPrice,
    [Range(0, 1_000_000_000)] decimal YearlyPrice,
    bool IsActive = true
);

// ─── Doanh thu E-Selling ──────────────────────────────────────────────────────

public class RevenuePointDto
{
    public string  Label  { get; init; } = "";   // "MM/yy"
    public decimal Amount { get; init; }
}

public class RevenueReportDto
{
    public decimal TotalRevenue        { get; init; }
    public decimal ThisMonthRevenue    { get; init; }
    public int     PaidOrders          { get; init; }
    public int     ActiveSubscriptions { get; init; }
    public List<RevenuePointDto> Monthly { get; init; } = new();
}

/// <summary>VM cho trang "Đăng ký các trường": biểu đồ doanh thu + danh sách đăng ký.</summary>
public class AllSubscriptionsVm
{
    public RevenueReportDto Revenue { get; init; } = new();
    public List<SchoolSubscriptionRowDto> Rows { get; init; } = new();
}

// ─── Onboard trường mới (SYSADMIN tạo Nhà trường) ─────────────────────────────

public record OnboardSchoolRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên trường."), MaxLength(150)] string SchoolName,
    [Required(ErrorMessage = "Vui lòng nhập tên người quản trị."), MaxLength(150)] string AdminFullName,
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(150)] string AdminEmail,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự."), MaxLength(100)] string AdminPassword,
    [MaxLength(30)] string? SchoolCode = null
);

// ─── Quản lý tài khoản trường (CRUD) ─────────────────────────────────────────

public class SchoolAccountDto
{
    public int      SchoolId           { get; init; }
    public string   SchoolCode         { get; init; } = "";
    public string   SchoolName         { get; init; } = "";
    public bool     IsActive           { get; init; }
    public int      AdminUserId        { get; init; }
    public string?  AdminFullName      { get; init; }
    public string?  AdminEmail         { get; init; }
    public bool     AdminActive        { get; init; }
    public string?  PlanName           { get; init; }
    public string   SubscriptionStatus { get; init; } = "None";
    public bool     SubscriptionActive { get; init; }
    public int      UserCount          { get; init; }
    public int      StudentCount       { get; init; }
    public int      ClassCount         { get; init; }
    public bool     CanDelete          { get; init; }   // chưa có dữ liệu học vụ
    public DateTime CreatedAt          { get; init; }
}

public record UpdateSchoolRequest(
    [Required] int SchoolId,
    [Required(ErrorMessage = "Vui lòng nhập tên trường."), MaxLength(150)] string SchoolName,
    [Required(ErrorMessage = "Vui lòng nhập tên quản trị."), MaxLength(150)] string AdminFullName,
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(150)] string AdminEmail,
    bool AdminActive = true
);
