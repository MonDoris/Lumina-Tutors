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

    /// <summary>Mua thêm gói slot tài khoản/lớp học (quota add-on) trên nền gói đang hoạt động.</summary>
    Task<Result<SubscriptionOrderDto>> BuyQuotaAddOnAsync(
        int schoolId, int userId, int quotaAddOnId, CancellationToken ct = default);

    /// <summary>Tạo đơn gia hạn kỳ tiếp theo (thủ công) cho gói hiện tại.</summary>
    Task<Result<SubscriptionOrderDto>> CreateRenewalOrderAsync(
        int schoolId, int userId, CancellationToken ct = default);

    /// <summary>Bật/tắt tự động gia hạn (chuyển giữa "đăng ký định kỳ" và "mua một lần").</summary>
    Task<Result> SetAutoRenewAsync(int schoolId, bool enabled, CancellationToken ct = default);

    /// <summary>Hủy đăng ký — không tự gia hạn nữa; vẫn dùng đến hết kỳ đã trả.</summary>
    Task<Result> CancelAsync(int schoolId, CancellationToken ct = default);

    /// <summary>
    /// Đặt email nhận hóa đơn của trường (School.Email). Để trống = xóa,
    /// khi đó hóa đơn gửi về email tài khoản Nhà trường.
    /// </summary>
    Task<Result> UpdateBillingEmailAsync(int schoolId, string? email, CancellationToken ct = default);

    Task<Result<SubscriptionOrderDto>> GetOrderAsync(int orderId, int schoolId, CancellationToken ct = default);

    /// <summary>Xác nhận thanh toán 1 đơn (idempotent) — kích hoạt gói/add-on, gia hạn kỳ.</summary>
    Task<SubscriptionConfirmResult> ConfirmOrderPaymentAsync(
        int orderId, decimal paidAmount, PaymentMethod method,
        string? transactionCode, string? gatewayRaw, CancellationToken ct = default);

    /// <summary>Trường có quyền dùng tính năng này không (gói bao gồm hoặc add-on còn hiệu lực)?</summary>
    Task<bool> HasFeatureAsync(int schoolId, PremiumFeature feature, CancellationToken ct = default);

    /// <summary>Trường có gói dịch vụ đang hoạt động (đã thanh toán, còn trong kỳ hiệu lực) không?</summary>
    Task<bool> HasActiveSubscriptionAsync(int schoolId, CancellationToken ct = default);

    /// <summary>Chi tiết một trường (SYSADMIN): thành viên theo vai trò + thống kê + phân bố xếp loại học lực.</summary>
    Task<Result<SchoolDetailDto>> GetSchoolDetailAsync(int schoolId, CancellationToken ct = default);

    /// <summary>SYSADMIN điều chỉnh gói trực tiếp cho một trường (kích hoạt ngay, ghi vào lịch sử).</summary>
    Task<Result> AdminChangePlanAsync(int schoolId, int planId, SubscriptionCycle cycle, bool autoRenew, int byUserId, CancellationToken ct = default);

    /// <summary>Danh sách các đơn đã thanh toán (cho dropdown chi tiết trên dashboard SYSADMIN).</summary>
    Task<Result<IReadOnlyList<PaidOrderDto>>> GetPaidOrdersAsync(CancellationToken ct = default);

    /// <summary>Đổi chu kỳ (tháng/quý/năm) của đơn mua gói chưa thanh toán — tính lại giá & kỳ hiệu lực.</summary>
    Task<Result> ChangeOrderCycleAsync(int orderId, int schoolId, SubscriptionCycle cycle, CancellationToken ct = default);

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
    Task<Result> SaveQuotaAddOnAsync(QuotaAddOnEditRequest request, CancellationToken ct = default);
    Task<Result> ToggleQuotaAddOnActiveAsync(int addOnId, CancellationToken ct = default);
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
