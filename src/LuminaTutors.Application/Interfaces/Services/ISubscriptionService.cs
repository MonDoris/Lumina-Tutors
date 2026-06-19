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

    /// <summary>
    /// Xử lý các đăng ký đã quá hạn: đánh dấu hết hạn; nếu bật auto-renew thì tự sinh
    /// đơn gia hạn (chờ thanh toán). Dùng cho cron/job định kỳ. Trả số đơn gia hạn đã tạo.
    /// </summary>
    Task<int> ProcessDueRenewalsAsync(CancellationToken ct = default);
}
