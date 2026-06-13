# AWS SES — Gửi Email (SendEmail)

## REST API trực tiếp

```
POST https://email.{region}.amazonaws.com/v2/email/outbound-emails
Content-Type: application/json
Authorization: AWS4-HMAC-SHA256 ...
x-amz-date: 20240612T143000Z
```

### Request Headers

| Header | Bắt buộc | Mô tả |
|--------|----------|-------|
| `Authorization` | ✅ | SigV4 signature |
| `x-amz-date` | ✅ | Timestamp UTC `yyyyMMddTHHmmssZ` |
| `Content-Type` | ✅ | `application/json` |

### Request Body (SESv2)

```json
{
  "FromEmailAddress": "no-reply@lumina.vn",
  "Destination": {
    "ToAddresses": ["hocvien@gmail.com"],
    "CcAddresses": [],
    "BccAddresses": []
  },
  "ReplyToAddresses": ["support@lumina.vn"],
  "Content": {
    "Simple": {
      "Subject": {
        "Data": "Thông báo học phí tháng 6/2024",
        "Charset": "UTF-8"
      },
      "Body": {
        "Text": {
          "Data": "Kính gửi học viên, học phí tháng 6 là 2.500.000đ...",
          "Charset": "UTF-8"
        },
        "Html": {
          "Data": "<h1>Thông báo học phí</h1><p>Kính gửi học viên...</p>",
          "Charset": "UTF-8"
        }
      }
    }
  },
  "EmailTags": [
    { "Name": "template", "Value": "fee-notification" },
    { "Name": "schoolId", "Value": "school-001" }
  ]
}
```

### Giải thích Request Body

| Field | Bắt buộc | Mô tả |
|-------|----------|-------|
| `FromEmailAddress` | ✅ | Địa chỉ gửi — phải được verify trong SES |
| `Destination.ToAddresses` | ✅ | Danh sách email người nhận (max 50) |
| `Destination.CcAddresses` | ❌ | CC |
| `Destination.BccAddresses` | ❌ | BCC |
| `ReplyToAddresses` | ❌ | Email reply-to |
| `Content.Simple.Subject.Data` | ✅ | Tiêu đề email |
| `Content.Simple.Body.Text.Data` | ❌ | Nội dung plaintext (fallback cho client không hỗ trợ HTML) |
| `Content.Simple.Body.Html.Data` | ❌ | Nội dung HTML |
| `EmailTags` | ❌ | Tags để tracking và filter trong dashboard |

> **Lưu ý:** Nên có cả `Text` và `Html` — client email sẽ chọn phiên bản phù hợp.

### Response

```json
{
  "MessageId": "0100018fa1b2c3d4-abc123def456-000001"
}
```

| Field | Mô tả |
|-------|-------|
| `MessageId` | ID email duy nhất — lưu lại để tracking bounce/complaint |

**HTTP Status:**
- `200 OK` — Email đã được SES nhận và đưa vào queue gửi
- `400 Bad Request` — Sai format hoặc địa chỉ email không hợp lệ
- `403 Forbidden` — Sai credentials hoặc email chưa verify (trong Sandbox)
- `429 Too Many Requests` — Vượt sending rate limit

---

## SDK .NET — Cách thông dụng nhất

### Service đơn giản

```csharp
public class EmailService : IEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IAmazonSimpleEmailServiceV2 sesClient, IConfiguration config)
    {
        _sesClient = sesClient;
        _fromEmail = config["AWS:SES:FromEmail"];
        _fromName  = config["AWS:SES:FromName"];
    }

    public async Task<string> SendAsync(EmailMessage message)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = $"{_fromName} <{_fromEmail}>",
            Destination = new Destination
            {
                ToAddresses = message.To.ToList(),
                CcAddresses = message.Cc?.ToList() ?? [],
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = message.Subject, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Html = new Content { Data = message.HtmlBody, Charset = "UTF-8" },
                        Text = new Content { Data = message.TextBody ?? StripHtml(message.HtmlBody), Charset = "UTF-8" }
                    }
                }
            },
            EmailTags = message.Tags?.Select(t => new MessageTag
            {
                Name  = t.Key,
                Value = t.Value
            }).ToList()
        };

        var response = await _sesClient.SendEmailAsync(request);
        return response.MessageId;  // Lưu vào DB để tracking
    }
}
```

### Gửi email từ HTML Template

```csharp
public async Task SendFeeNotificationAsync(Student student, decimal amount, string month)
{
    var htmlBody = await _templateService.RenderAsync("fee-notification", new
    {
        StudentName = student.FullName,
        Amount      = amount.ToString("N0"),
        Month       = month,
        DueDate     = DateTime.Now.AddDays(7).ToString("dd/MM/yyyy"),
        PaymentUrl  = $"https://lumina.vn/pay/{student.Id}"
    });

    await SendAsync(new EmailMessage
    {
        To       = [student.Email],
        Subject  = $"Thông báo học phí tháng {month}",
        HtmlBody = htmlBody,
        Tags     = new() { ["type"] = "fee", ["schoolId"] = student.SchoolId }
    });
}
```

---

## Sending Limits

| Metric | Sandbox | Production (default) |
|--------|---------|---------------------|
| Email/ngày | 200 | 50,000 |
| Email/giây | 1 | 14 |
| Tăng limit | Không được | Gửi request cho AWS |

> Với trung tâm vừa (~500 học viên), default production limit là đủ.
