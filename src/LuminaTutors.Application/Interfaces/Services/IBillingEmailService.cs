using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Email nghiệp vụ thanh toán gói dịch vụ (E-Selling) gửi về NHÀ TRƯỜNG.
/// Gọi tự động sau khi đơn được xác nhận thanh toán (VNPay hoặc xác nhận thủ công),
/// và có thể gọi lại thủ công từ trang Gói dịch vụ ("Gửi lại hóa đơn").
/// </summary>
public interface IBillingEmailService
{
    /// <summary>
    /// Gửi hóa đơn/biên nhận của một đơn đã thanh toán về email nhà trường
    /// (School.Email; nếu trống thì gửi tài khoản Nhà trường đang hoạt động).
    /// </summary>
    Task<Result> SendSubscriptionReceiptAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// Gửi email thử tới đúng địa chỉ sẽ nhận hóa đơn — dùng để kiểm tra cấu hình SMTP
    /// mà không cần phát sinh giao dịch. Trả về địa chỉ đã gửi.
    /// </summary>
    Task<Result<string>> SendTestEmailAsync(int schoolId, CancellationToken ct = default);
}
