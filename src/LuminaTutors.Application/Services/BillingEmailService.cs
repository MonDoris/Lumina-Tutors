using System.Globalization;
using System.Net;
using System.Text;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Subscription;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

/// <summary>
/// Dựng và gửi hóa đơn gói dịch vụ về nhà trường sau khi thanh toán thành công.
/// Email dạng HTML inline-style (client email không đọc &lt;style&gt; ngoài) kèm bản text dự phòng.
/// </summary>
public sealed class BillingEmailService : IBillingEmailService
{
    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");

    private readonly IUnitOfWork  _uow;
    private readonly IEmailSender _mail;
    private readonly ILogger<BillingEmailService> _logger;

    public BillingEmailService(IUnitOfWork uow, IEmailSender mail, ILogger<BillingEmailService> logger)
    {
        _uow    = uow;
        _mail   = mail;
        _logger = logger;
    }

    public async Task<Result> SendSubscriptionReceiptAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _uow.SubscriptionOrders.FindOneAsync(
            o => o.Id == orderId,
            include: q => q.Include(o => o.Items)
                           .Include(o => o.Plan)
                           .Include(o => o.Subscription).ThenInclude(s => s.Plan),
            ct: ct);

        if (order is null)
            return Result.Failure("Không tìm thấy đơn dịch vụ.", "ORDER_NOT_FOUND");
        if (order.Status != SubscriptionOrderStatus.Paid)
            return Result.Failure("Đơn chưa được thanh toán — chưa thể gửi hóa đơn.", "ORDER_NOT_PAID");

        var school = await _uow.Schools.GetByIdAsync(order.SchoolId, ct);
        if (school is null)
            return Result.Failure("Không tìm thấy trường của đơn.", "SCHOOL_NOT_FOUND");

        var (to, toName, cc) = await ResolveRecipientAsync(school, order, ct);
        if (to is null)
        {
            _logger.LogWarning("Không có email nhận hóa đơn cho trường {SchoolId} (đơn {Order})",
                school.Id, order.OrderCode);
            return Result.Failure(
                "Trường chưa có email nhận hóa đơn. Hãy cập nhật email trường hoặc email tài khoản Nhà trường.",
                "NO_RECIPIENT");
        }

        var subject = $"[Lumina Tutors] Hóa đơn {order.OrderCode} — đã thanh toán {Money(order.Amount)}";
        var message = new EmailMessage(to, toName, subject,
            BuildHtml(order, school), BuildText(order, school), cc);

        var sent = await _mail.SendAsync(message, ct);
        if (sent.IsSuccess)
            _logger.LogInformation("Đã gửi hóa đơn {Order} tới {To} (trường {SchoolId})", order.OrderCode, to, school.Id);
        else
            _logger.LogError("Gửi hóa đơn {Order} tới {To} thất bại: {Error}", order.OrderCode, to, sent.Error);

        return sent;
    }

    public async Task<Result<string>> SendTestEmailAsync(int schoolId, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null)
            return Result<string>.Failure("Không tìm thấy trường.", "SCHOOL_NOT_FOUND");

        var (to, toName, _) = await ResolveRecipientAsync(school, null, ct);
        if (to is null)
            return Result<string>.Failure(
                "Chưa có email nhận hóa đơn. Hãy nhập email nhận hóa đơn rồi thử lại.", "NO_RECIPIENT");

        var html = $$"""
<div style="margin:0;padding:24px 12px;background:#f1f5f9;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif">
  <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:14px;overflow:hidden">
    <div style="background:#0f1e35;padding:20px 24px">
      <div style="color:#d4af37;font-size:13px;letter-spacing:.14em;font-weight:700">LUMINA TUTORS</div>
      <div style="color:#ffffff;font-size:18px;font-weight:700;margin-top:6px">Email thử — cấu hình gửi thư hoạt động</div>
    </div>
    <div style="padding:22px 24px;font-size:14px;color:#334155;line-height:1.7">
      <p style="margin:0 0 10px">Chào {{Enc(school.SchoolName)}},</p>
      <p style="margin:0 0 10px">Đây là email thử từ hệ thống Lumina Tutors. Nếu bạn nhận được thư này,
         hóa đơn gói dịch vụ sẽ được gửi về <strong>{{Enc(to)}}</strong> sau mỗi lần thanh toán thành công.</p>
      <p style="margin:0;color:#64748b;font-size:13px">Thời điểm gửi: {{DateTime.Now.ToString("HH:mm dd/MM/yyyy", Vn)}}</p>
    </div>
  </div>
</div>
""";

        var text = $"Email thử từ Lumina Tutors. Hóa đơn gói dịch vụ sẽ được gửi về {to}.";
        var sent = await _mail.SendAsync(
            new EmailMessage(to, toName, "[Lumina Tutors] Email thử — cấu hình gửi hóa đơn", html, text), ct);

        return sent.IsSuccess
            ? Result<string>.Success(to)
            : Result<string>.Failure(sent.Error ?? "Không gửi được email thử.", sent.ErrorCode ?? "EMAIL_SEND_FAILED");
    }

    // ── Người nhận: email trường → fallback tài khoản Nhà trường ──────────────

    private async Task<(string? To, string? ToName, string? Cc)> ResolveRecipientAsync(
        School school, SubscriptionOrder? order, CancellationToken ct)
    {
        string? to     = IsEmail(school.Email) ? school.Email!.Trim() : null;
        string? toName = school.SchoolName;

        if (to is null)
        {
            var admins = await _uow.Users.FindAsync(
                u => u.SchoolId == school.Id && u.IsActive && u.Role.RoleCode == "ADMIN",
                include: q => q.Include(u => u.Role), ct: ct);

            var admin = admins.FirstOrDefault(u => IsEmail(u.Email));
            if (admin is not null) { to = admin.Email.Trim(); toName = admin.FullName; }
        }

        // CC người bấm mua (nếu khác địa chỉ nhận chính) để họ có bản lưu.
        string? cc = null;
        if (order?.CreatedByUserId is not null)
        {
            var buyer = await _uow.Users.GetByIdAsync(order.CreatedByUserId.Value, ct);
            if (buyer is not null && IsEmail(buyer.Email) &&
                !string.Equals(buyer.Email, to, StringComparison.OrdinalIgnoreCase))
                cc = buyer.Email.Trim();
        }

        return (to, toName, cc);
    }

    private static bool IsEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && !value.Trim().EndsWith("@");

    // ── Nội dung ──────────────────────────────────────────────────────────────

    private static string BuildHtml(SubscriptionOrder order, School school)
    {
        var rows = new StringBuilder();
        foreach (var item in OrderLines(order))
        {
            rows.Append(
                "<tr>" +
                $"<td style=\"padding:10px 12px;border-bottom:1px solid #eef0f4;font-size:14px;color:#1f2937\">{Enc(item.Description)}</td>" +
                $"<td style=\"padding:10px 12px;border-bottom:1px solid #eef0f4;font-size:14px;color:#1f2937;text-align:right;white-space:nowrap\">{Enc(Money(item.Amount))}</td>" +
                "</tr>");
        }

        var infoRows = new StringBuilder();
        void Info(string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            infoRows.Append(
                "<tr>" +
                $"<td style=\"padding:4px 0;font-size:13px;color:#6b7280;width:46%\">{Enc(label)}</td>" +
                $"<td style=\"padding:4px 0;font-size:13px;color:#1f2937;font-weight:600\">{Enc(value)}</td>" +
                "</tr>");
        }

        Info("Mã hóa đơn",     order.OrderCode);
        Info("Loại giao dịch", OrderTypeVi(order.OrderType));
        Info("Gói dịch vụ",    order.Plan?.Name ?? order.Subscription?.Plan?.Name);
        Info("Chu kỳ",         CycleVi(order.BillingCycle));
        Info("Kỳ hiệu lực",    $"{order.PeriodStart:dd/MM/yyyy} – {order.PeriodEnd:dd/MM/yyyy}");
        Info("Hình thức thanh toán", PaymentVi(order.PaymentMethod));
        Info("Mã giao dịch",   order.TransactionCode);
        Info("Thời điểm thanh toán", order.PaidAt?.ToLocalTime().ToString("HH:mm dd/MM/yyyy", Vn));

        return $$"""
<div style="margin:0;padding:24px 12px;background:#f1f5f9;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif">
  <div style="max-width:620px;margin:0 auto;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 6px 24px rgba(15,23,42,.08)">

    <div style="background:#0f1e35;padding:22px 26px">
      <div style="color:#d4af37;font-size:13px;letter-spacing:.14em;font-weight:700">LUMINA TUTORS</div>
      <div style="color:#ffffff;font-size:20px;font-weight:700;margin-top:6px">Hóa đơn thanh toán gói dịch vụ</div>
      <div style="color:#9fb0c9;font-size:13px;margin-top:4px">Cảm ơn {{Enc(school.SchoolName)}} đã tin dùng Lumina Tutors.</div>
    </div>

    <div style="padding:22px 26px">
      <div style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:10px;padding:14px 16px;margin-bottom:18px">
        <div style="font-size:13px;color:#15803d;font-weight:700">✔ Thanh toán thành công</div>
        <div style="font-size:26px;color:#14532d;font-weight:800;margin-top:4px">{{Enc(Money(order.Amount))}}</div>
      </div>

      <table style="width:100%;border-collapse:collapse;margin-bottom:20px">{{infoRows}}</table>

      <div style="font-size:13px;color:#6b7280;font-weight:700;text-transform:uppercase;letter-spacing:.06em;margin-bottom:6px">Chi tiết đơn</div>
      <table style="width:100%;border-collapse:collapse;border:1px solid #eef0f4;border-radius:8px">
        {{rows}}
        <tr>
          <td style="padding:12px;font-size:14px;font-weight:700;color:#0f1e35">Tổng cộng</td>
          <td style="padding:12px;font-size:16px;font-weight:800;color:#0f1e35;text-align:right;white-space:nowrap">{{Enc(Money(order.Amount))}}</td>
        </tr>
      </table>

      <div style="margin-top:20px;padding-top:16px;border-top:1px solid #eef0f4;font-size:13px;color:#6b7280;line-height:1.7">
        <div><strong style="color:#1f2937">Đơn vị:</strong> {{Enc(school.SchoolName)}}{{(string.IsNullOrWhiteSpace(school.Address) ? "" : " — " + Enc(school.Address!))}}</div>
        {{(string.IsNullOrWhiteSpace(school.PhoneNumber) ? "" : $"<div><strong style=\"color:#1f2937\">Điện thoại:</strong> {Enc(school.PhoneNumber!)}</div>")}}
        <div style="margin-top:10px">Gói và các tính năng đi kèm đã được kích hoạt cho tài khoản trường. Email này là biên nhận tự động — vui lòng lưu lại để đối chiếu.</div>
      </div>
    </div>

    <div style="background:#f8fafc;padding:14px 26px;font-size:12px;color:#94a3b8;text-align:center">
      Lumina Tutors — Hệ thống quản lý giáo dục. Email tự động, vui lòng không trả lời.
    </div>
  </div>
</div>
""";
    }

    private static string BuildText(SubscriptionOrder order, School school)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LUMINA TUTORS — HÓA ĐƠN THANH TOÁN GÓI DỊCH VỤ");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"Đơn vị:        {school.SchoolName}");
        sb.AppendLine($"Mã hóa đơn:    {order.OrderCode}");
        sb.AppendLine($"Loại giao dịch:{OrderTypeVi(order.OrderType)}");
        sb.AppendLine($"Gói dịch vụ:   {order.Plan?.Name ?? order.Subscription?.Plan?.Name ?? "—"}");
        sb.AppendLine($"Chu kỳ:        {CycleVi(order.BillingCycle)}");
        sb.AppendLine($"Kỳ hiệu lực:   {order.PeriodStart:dd/MM/yyyy} – {order.PeriodEnd:dd/MM/yyyy}");
        sb.AppendLine($"Thanh toán:    {PaymentVi(order.PaymentMethod)}");
        if (!string.IsNullOrWhiteSpace(order.TransactionCode))
            sb.AppendLine($"Mã giao dịch:  {order.TransactionCode}");
        if (order.PaidAt.HasValue)
            sb.AppendLine($"Thời điểm:     {order.PaidAt.Value.ToLocalTime():HH:mm dd/MM/yyyy}");
        sb.AppendLine(new string('-', 48));
        foreach (var line in OrderLines(order))
            sb.AppendLine($"• {line.Description}: {Money(line.Amount)}");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"TỔNG CỘNG:     {Money(order.Amount)}");
        sb.AppendLine();
        sb.AppendLine("Gói và tính năng đi kèm đã được kích hoạt. Email tự động, vui lòng không trả lời.");
        return sb.ToString();
    }

    /// <summary>Các dòng của hóa đơn; đơn không có Items thì dựng 1 dòng từ gói.</summary>
    private static IEnumerable<(string Description, decimal Amount)> OrderLines(SubscriptionOrder order)
    {
        if (order.Items.Count > 0)
            return order.Items.Select(i => (i.Description, i.Amount)).ToList();

        var name = order.Plan?.Name ?? order.Subscription?.Plan?.Name ?? "Gói dịch vụ";
        return new List<(string, decimal)> { ($"{name} — {CycleVi(order.BillingCycle)}", order.Amount) };
    }

    // ── Nhãn tiếng Việt ───────────────────────────────────────────────────────

    private static string Money(decimal amount) => amount.ToString("N0", Vn) + "đ";

    private static string OrderTypeVi(SubscriptionOrderType type) => type switch
    {
        SubscriptionOrderType.New     => "Đăng ký mới",
        SubscriptionOrderType.Upgrade => "Nâng cấp gói",
        SubscriptionOrderType.AddOn   => "Mua thêm tính năng",
        SubscriptionOrderType.Renewal => "Gia hạn",
        _                             => type.ToString()
    };

    private static string CycleVi(SubscriptionCycle cycle) => cycle switch
    {
        SubscriptionCycle.Monthly   => "Theo tháng",
        SubscriptionCycle.Quarterly => "Theo quý",
        SubscriptionCycle.Yearly    => "Theo năm",
        _                           => cycle.ToString()
    };

    private static string PaymentVi(PaymentMethod? method) => method switch
    {
        PaymentMethod.VnPay        => "VNPay",
        PaymentMethod.Momo         => "Momo",
        PaymentMethod.ZaloPay      => "ZaloPay",
        PaymentMethod.BankTransfer => "Chuyển khoản / xác nhận thủ công",
        PaymentMethod.Cash         => "Tiền mặt",
        _                          => "—"
    };

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
