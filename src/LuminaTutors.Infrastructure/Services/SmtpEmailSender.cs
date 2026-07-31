using System.Text;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace LuminaTutors.Infrastructure.Services;

/// <summary>
/// Gửi email qua SMTP bằng MailKit. Hỗ trợ 2 cách xác thực (chọn ở <c>Email:Provider</c>):
///
///  • <c>"AppPassword"</c> (mặc định) — đăng nhập bằng <c>Email:Username</c> + <c>Email:Password</c>
///    (App Password 16 ký tự của Gmail).
///  • <c>"GmailOAuth"</c> — xác thực XOAUTH2 bằng access token đổi từ <c>Email:RefreshToken</c>
///    (dùng OAuth client: ClientId/ClientSecret/RefreshToken). Không cần App Password.
///
/// <c>Email:Enabled = false</c> hoặc thiếu Host/FromEmail → KHÔNG gửi thật: email được ghi ra
/// file .html trong <c>Email:DevDropFolder</c> (mặc định <c>logs/emails</c>) để xem thử.
/// Mọi bí mật (Password, ClientSecret, RefreshToken) đặt bằng user-secrets, KHÔNG commit.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration       _config;
    private readonly IGmailTokenProvider  _gmailToken;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, IGmailTokenProvider gmailToken, ILogger<SmtpEmailSender> logger)
    {
        _config     = config;
        _gmailToken = gmailToken;
        _logger     = logger;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.ToEmail))
            return Result.Failure("Thiếu địa chỉ email người nhận.", "EMAIL_NO_RECIPIENT");

        var enabled = _config.GetValue("Email:Enabled", false);
        var host    = _config["Email:Host"];
        var from    = _config["Email:FromEmail"];

        if (!enabled || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            return await DropToFolderAsync(message, ct);

        var port     = _config.GetValue("Email:Port", 587);
        var ssl      = _config.GetValue("Email:EnableSsl", true);
        var fromName = _config["Email:FromName"] ?? "Lumina Tutors";
        var provider = (_config["Email:Provider"] ?? "AppPassword").Trim();
        var user     = _config["Email:Username"];

        var mime = BuildMessage(message, from, fromName);

        // 465 = SSL ngầm; 587/25 = STARTTLS.
        var socketOption = port == 465
            ? SecureSocketOptions.SslOnConnect
            : (ssl ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable);

        try
        {
            using var client = new SmtpClient { Timeout = 20_000 };
            await client.ConnectAsync(host, port, socketOption, ct);

            var auth = await AuthenticateAsync(client, provider, user, ct);
            if (!auth.IsSuccess)
            {
                await client.DisconnectAsync(true, ct);
                return auth;
            }

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Đã gửi email '{Subject}' tới {To} ({Provider})",
                message.Subject, message.ToEmail, provider);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email '{Subject}' tới {To} thất bại", message.Subject, message.ToEmail);
            return Result.Failure($"Không gửi được email: {ex.Message}", "EMAIL_SEND_FAILED");
        }
    }

    // ── Xác thực theo chế độ ──────────────────────────────────────────────────

    private async Task<Result> AuthenticateAsync(SmtpClient client, string provider, string? user, CancellationToken ct)
    {
        if (provider.Equals("GmailOAuth", StringComparison.OrdinalIgnoreCase))
        {
            var account = user ?? _config["Email:FromEmail"];
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure("Thiếu Email:Username (địa chỉ Gmail) cho chế độ OAuth.", "EMAIL_NO_USER");

            var token = await _gmailToken.GetAccessTokenAsync(ct);
            if (string.IsNullOrWhiteSpace(token))
                return Result.Failure(
                    "Không lấy được access token từ refresh token. Kiểm tra Email:ClientId / ClientSecret / RefreshToken (user-secrets).",
                    "EMAIL_OAUTH_FAILED");

            await client.AuthenticateAsync(new SaslMechanismOAuth2(account, token), ct);
            return Result.Success();
        }

        // AppPassword / basic auth
        var password = _config["Email:Password"];
        if (string.IsNullOrWhiteSpace(user))
            return Result.Success();   // SMTP không cần đăng nhập (relay nội bộ)

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError("Email:Password trống — chưa đặt App Password cho {User}", user);
            return Result.Failure(
                $"Chưa cấu hình mật khẩu email cho {user}. Chạy: dotnet user-secrets set \"Email:Password\" \"<App Password>\" --project src/LuminaTutors.Web",
                "EMAIL_NO_PASSWORD");
        }

        await client.AuthenticateAsync(user, password, ct);
        return Result.Success();
    }

    private static MimeMessage BuildMessage(EmailMessage message, string from, string fromName)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(fromName, from));
        mime.To.Add(string.IsNullOrWhiteSpace(message.ToName)
            ? MailboxAddress.Parse(message.ToEmail)
            : new MailboxAddress(message.ToName, message.ToEmail));
        if (!string.IsNullOrWhiteSpace(message.CcEmail))
            mime.Cc.Add(MailboxAddress.Parse(message.CcEmail));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (!string.IsNullOrWhiteSpace(message.TextBody))
            builder.TextBody = message.TextBody;
        mime.Body = builder.ToMessageBody();
        return mime;
    }

    /// <summary>Chưa bật SMTP: lưu email ra file để xem thử (dev) và coi như gửi thành công.</summary>
    private async Task<Result> DropToFolderAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            var folder = _config["Email:DevDropFolder"];
            if (string.IsNullOrWhiteSpace(folder))
                folder = Path.Combine(AppContext.BaseDirectory, "logs", "emails");
            Directory.CreateDirectory(folder);

            var safeTo = string.Concat(message.ToEmail.Split(Path.GetInvalidFileNameChars()));
            var file   = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd-HHmmss}-{safeTo}.html");

            var header =
                $"<!-- To: {message.ToEmail} | Cc: {message.CcEmail ?? "-"} | Subject: {message.Subject} -->\n";
            await File.WriteAllTextAsync(file, header + message.HtmlBody, Encoding.UTF8, ct);

            _logger.LogWarning(
                "Email chưa bật (Email:Enabled=false hoặc thiếu Host/FromEmail) — đã lưu '{Subject}' cho {To} vào {File}",
                message.Subject, message.ToEmail, file);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không ghi được file email dự phòng cho {To}", message.ToEmail);
            return Result.Failure("Không gửi được email (SMTP chưa cấu hình).", "EMAIL_NOT_CONFIGURED");
        }
    }
}
