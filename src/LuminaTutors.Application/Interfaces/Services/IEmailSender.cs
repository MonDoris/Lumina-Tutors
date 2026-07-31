using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>Một email chuẩn bị gửi đi (HTML + bản text dự phòng).</summary>
public sealed record EmailMessage(
    string  ToEmail,
    string? ToName,
    string  Subject,
    string  HtmlBody,
    string? TextBody = null,
    string? CcEmail  = null
);

/// <summary>
/// Cổng gửi email của hệ thống. Hiện thực nằm ở tầng Infrastructure (SMTP);
/// khi cấu hình <c>Email:Enabled = false</c> thì email được ghi ra file để xem thử
/// trong môi trường phát triển thay vì gửi thật.
/// </summary>
public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default);
}
