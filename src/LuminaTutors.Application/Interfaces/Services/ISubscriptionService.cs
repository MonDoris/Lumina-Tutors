using LuminaTutors.Application.DTOs.Subscription;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Quản lý gói dịch vụ cấp trường (SaaS): nâng cấp gói, mua add-on (AI Tutor /
/// Virtual Lab), đăng ký định kỳ tự gia hạn, và kiểm tra quyền dùng tính năng.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Toàn bộ dữ liệu cho trang quản lý gói: trạng thái hiện tại + catalog + đơn gần đây.</summary>
    Task<Result<SubscriptionOverviewDto>> GetOverviewAsync(int schoolId, CancellationToken ct = default);

    /// <summary>Đăng ký mới hoặc nâng cấp lên gói cao hơn → tạo đơn chờ thanh toán.</summary>
    Task<Result<SubscriptionOrderDto>> SubscribeOrUpgradeAsync(
        int schoolId, int userId, ChangePlanRequest request, CancellationToken ct = default);

    /// <summary>Mua thêm tính năng lẻ (add-on) trên nền gói đang hoạt động.</summary>
    Task<Result<SubscriptionOrderDto>> BuyAddOnAsync(
        int schoolId, int userId, int addOnId, CancellationToken ct = default);

    /// <summary>Tạo đơn gia hạn kỳ tiếp theo (thủ công) cho gói hiện tại.</summary>
    Task<Result<SubscriptionOrderDto>> CreateRenewalOrderAsync(
        int schoolId, int userId, CancellationToken ct = default);

    /// <summary>Bật/tắt tự động gia hạn (chuyển giữa "đăng ký định kỳ" và "mua một lần").</summary>
    Task<Result> SetAutoRenewAsync(int schoolId, bool enabled, CancellationToken ct = default);

    /// <summary>Hủy đăng ký — không tự gia hạn nữa; vẫn dùng đến hết kỳ đã trả.</summary>
    Task<Result> CancelAsync(int schoolId, CancellationToken ct = default);

    Task<Result<SubscriptionOrderDto>> GetOrderAsync(int orderId, int schoolId, CancellationToken ct = default);

    /// <summary>Xác nhận thanh toán 1 đơn (idempotent) — kích hoạt gói/add-on, gia hạn kỳ.</summary>
    Task<SubscriptionConfirmResult> ConfirmOrderPaymentAsync(
        int orderId, decimal paidAmount, PaymentMethod method,
        string? transactionCode, string? gatewayRaw, CancellationToken ct = default);

    /// <summary>Trường có quyền dùng tính năng này không (gói bao gồm hoặc add-on còn hiệu lực)?</summary>
    Task<bool> HasFeatureAsync(int schoolId, PremiumFeature feature, CancellationToken ct = default);

    /// <summary>Trường có gói dịch vụ đang hoạt động (đã thanh toán, còn trong kỳ hiệu lực) không?</summary>
    Task<bool> HasActiveSubscriptionAsync(int schoolId, CancellationToken ct = default);

    /// <summary>
    /// Xử lý các đăng ký đã quá hạn: đánh dấu hết hạn; nếu bật auto-renew thì tự sinh
    /// đơn gia hạn (chờ thanh toán). Dùng cho cron/job định kỳ. Trả số đơn gia hạn đã tạo.
    /// </summary>
    Task<int> ProcessDueRenewalsAsync(CancellationToken ct = default);

    // ─── E-Selling admin (SYSADMIN) ──────────────────────────────────────────
    // Quản lý "bên bán": catalog gói/add-on và xem đăng ký của mọi trường.

    Task<Result<CatalogDto>> GetCatalogAsync(CancellationToken ct = default);
    Task<Result> SavePlanAsync(PlanEditRequest request, CancellationToken ct = default);
    Task<Result> TogglePlanActiveAsync(int planId, CancellationToken ct = default);
    Task<Result> SaveAddOnAsync(AddOnEditRequest request, CancellationToken ct = default);
    Task<Result> ToggleAddOnActiveAsync(int addOnId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SchoolSubscriptionRowDto>>> GetAllSubscriptionsAsync(CancellationToken ct = default);

    /// <summary>Báo cáo doanh thu E-Selling (đơn đã thanh toán) + chuỗi 12 tháng gần nhất.</summary>
    Task<Result<RevenueReportDto>> GetRevenueReportAsync(CancellationToken ct = default);

    /// <summary>Tạo trường mới + tài khoản Nhà trường (ADMIN) cho trường đó.</summary>
    Task<Result> OnboardSchoolAsync(OnboardSchoolRequest request, CancellationToken ct = default);

    // ─── Quản lý tài khoản trường (CRUD) ─────────────────────────────────────
    Task<Result<IReadOnlyList<SchoolAccountDto>>> GetSchoolsAsync(CancellationToken ct = default);
    Task<Result> UpdateSchoolAsync(UpdateSchoolRequest request, CancellationToken ct = default);
    Task<Result> ToggleSchoolActiveAsync(int schoolId, int currentSchoolId, CancellationToken ct = default);
    Task<Result> ResetSchoolAdminPasswordAsync(int schoolId, string newPassword, CancellationToken ct = default);
    Task<Result> DeleteSchoolAsync(int schoolId, int currentSchoolId, CancellationToken ct = default);
}
