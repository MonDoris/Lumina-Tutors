# AWS SES — Cấu hình SMTP

## Khi nào dùng SMTP?

Dùng SMTP khi cần tích hợp với hệ thống cũ hoặc thư viện chỉ hỗ trợ SMTP chuẩn (như MailKit, FluentEmail).

---

## SMTP Endpoints theo Region

| Region | SMTP Endpoint | Port |
|--------|--------------|------|
| ap-southeast-1 (Singapore) | `email-smtp.ap-southeast-1.amazonaws.com` | 587 (TLS) / 465 (SSL) / 25 |
| us-east-1 | `email-smtp.us-east-1.amazonaws.com` | 587 / 465 / 25 |

**Khuyến nghị:** Dùng port **587** với STARTTLS.

---

## Tạo SMTP Credentials

> SMTP credentials **khác** với AWS Access Key. Phải tạo riêng trong SES Console.

```
SES Console → SMTP Settings → Create SMTP Credentials
→ Tên IAM user: lumina-ses-smtp
→ Download credentials (chỉ hiện 1 lần)
```

Format credentials:
- **SMTP Username:** `AKIAIOSFODNN7EXAMPLE` (dạng Access Key ID)
- **SMTP Password:** `BXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx` (dài hơn, đã convert sang SMTP format)

---

## Cấu hình trong ASP.NET Core (FluentEmail + MailKit)

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package MailKit
```

```csharp
// Program.cs
builder.Services
    .AddFluentEmail(config["AWS:SES:FromEmail"], config["AWS:SES:FromName"])
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient
    {
        Host            = "email-smtp.ap-southeast-1.amazonaws.com",
        Port            = 587,
        EnableSsl       = true,  // STARTTLS
        DeliveryMethod  = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials     = new NetworkCredential(
            config["AWS:SES:SmtpUsername"],
            config["AWS:SES:SmtpPassword"]
        )
    });
```

### Gửi email với FluentEmail

```csharp
public class EmailService
{
    private readonly IFluentEmail _email;

    public async Task SendFeeNotificationAsync(string to, FeeModel model)
    {
        await _email
            .To(to)
            .Subject($"Thông báo học phí tháng {model.Month}")
            .UsingTemplate(@"
                <h2>Kính gửi @Model.StudentName</h2>
                <p>Học phí tháng @Model.Month: <strong>@Model.Amount VNĐ</strong></p>
                <p>Hạn nộp: @Model.DueDate</p>
                <a href=""@Model.PayUrl"">Thanh toán ngay</a>
            ", model)
            .SendAsync();
    }
}
```

---

## Cấu hình với MailKit trực tiếp

```csharp
using var client = new MailKit.Net.Smtp.SmtpClient();

await client.ConnectAsync(
    "email-smtp.ap-southeast-1.amazonaws.com",
    587,
    MailKit.Security.SecureSocketOptions.StartTls
);

await client.AuthenticateAsync(
    smtpUsername,
    smtpPassword
);

var message = new MimeMessage();
message.From.Add(new MailboxAddress("Lumina Tutors", "no-reply@lumina.vn"));
message.To.Add(new MailboxAddress("Học viên", "hocvien@gmail.com"));
message.Subject = "Thông báo học phí";

var builder = new BodyBuilder
{
    HtmlBody = "<h1>Nội dung HTML</h1>",
    TextBody = "Nội dung text"
};
// Attachment
builder.Attachments.Add("hoadon.pdf", pdfBytes, ContentType.Parse("application/pdf"));

message.Body = builder.ToMessageBody();

await client.SendAsync(message);
await client.DisconnectAsync(true);
```

---

## SDK vs SMTP — So sánh

| | AWS SDK | SMTP |
|--|---------|------|
| Hiệu suất | Tốt hơn (HTTP/2) | Kém hơn (kết nối TCP) |
| Attachment | Dùng SendRawEmail | Native |
| Retry logic | Tự xử lý | Tự xử lý |
| MessageId để tracking | ✅ | ❌ |
| Khuyến nghị | ✅ Cho app mới | Cho hệ thống cũ |
