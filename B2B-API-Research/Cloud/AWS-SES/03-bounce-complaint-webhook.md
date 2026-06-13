# AWS SES — Bounce & Complaint Webhook (SNS)

## Bounce và Complaint là gì?

| Loại | Ý nghĩa | Hậu quả nếu không xử lý |
|------|---------|-------------------------|
| **Bounce** | Email không đến được (địa chỉ không tồn tại, hộp thư đầy) | Tăng bounce rate → AWS đình chỉ tài khoản |
| **Complaint** | Người nhận đánh dấu là spam | Tăng complaint rate → AWS đình chỉ |

**AWS yêu cầu:** Bounce rate < 5%, Complaint rate < 0.1%. Phải xử lý để dừng gửi đến địa chỉ xấu.

---

## Luồng hoạt động

```
SES gửi email
     │
     └─► Email bounce/complaint
               │
          SES → SNS Topic → HTTP Endpoint (Lumina Server)
                                    │
                            Cập nhật DB: đánh dấu email bad
```

---

## Cấu hình SNS Webhook

### Bước 1: Tạo SNS Topic trong AWS Console

```
SNS → Topics → Create Topic → Standard → Name: lumina-ses-notifications
```

### Bước 2: Subscribe HTTP endpoint

```
SNS → Subscriptions → Create Subscription
  Protocol: HTTPS
  Endpoint: https://lumina.vn/webhooks/ses-notifications
```

### Bước 3: Liên kết SES với SNS

```
SES → Identities → {your-domain} → Notifications
  Bounces → SNS Topic: lumina-ses-notifications
  Complaints → SNS Topic: lumina-ses-notifications
```

---

## Webhook Endpoint

### Request từ SNS

```
POST https://lumina.vn/webhooks/ses-notifications
Content-Type: text/plain; charset=UTF-8
x-amz-sns-message-type: Notification
x-amz-sns-message-id: {uuid}
x-amz-sns-topic-arn: arn:aws:sns:ap-southeast-1:123456:lumina-ses-notifications
x-amz-sns-subscription-arn: arn:aws:sns:...:lumina-ses-notifications:{sub-id}
```

### Request Headers quan trọng

| Header | Mô tả |
|--------|-------|
| `x-amz-sns-message-type` | `SubscriptionConfirmation` (lần đầu) hoặc `Notification` |
| `x-amz-sns-topic-arn` | ARN của SNS topic — verify để tránh giả mạo |

### Body — Notification (Bounce)

```json
{
  "Type": "Notification",
  "MessageId": "uuid-from-sns",
  "TopicArn": "arn:aws:sns:...:lumina-ses-notifications",
  "Message": "{\"notificationType\":\"Bounce\",\"bounce\":{\"bounceType\":\"Permanent\",\"bounceSubType\":\"General\",\"bouncedRecipients\":[{\"emailAddress\":\"user@nonexistent.com\",\"action\":\"failed\",\"status\":\"5.1.1\",\"diagnosticCode\":\"smtp; 550 5.1.1 user unknown\"}],\"timestamp\":\"2024-06-12T14:30:00.000Z\",\"feedbackId\":\"abc123\"},\"mail\":{\"messageId\":\"0100018fa1b2c3d4-abc123def456-000001\",\"source\":\"no-reply@lumina.vn\",\"timestamp\":\"2024-06-12T14:29:55.000Z\",\"destination\":[\"user@nonexistent.com\"]}}",
  "Timestamp": "2024-06-12T14:30:01.000Z"
}
```

### Message.bounce object

| Field | Mô tả |
|-------|-------|
| `bounceType` | `Permanent` (địa chỉ không tồn tại) hoặc `Transient` (tạm thời) |
| `bounceSubType` | `General`, `NoEmail`, `Suppressed`, `MailboxFull` |
| `bouncedRecipients[].emailAddress` | Email bị bounce |
| `feedbackId` | ID duy nhất của feedback |
| `mail.messageId` | MessageId lúc gửi — dùng để tìm trong DB |

### Body — Notification (Complaint)

```json
{
  "Message": "{\"notificationType\":\"Complaint\",\"complaint\":{\"complainedRecipients\":[{\"emailAddress\":\"user@example.com\"}],\"complaintFeedbackType\":\"abuse\",\"timestamp\":\"2024-06-12T14:30:00.000Z\",\"feedbackId\":\"xyz789\"},\"mail\":{\"messageId\":\"0100018fa1...\",\"destination\":[\"user@example.com\"]}}"
}
```

---

## Xử lý Webhook — Code mẫu C#

```csharp
[HttpPost("webhooks/ses-notifications")]
[AllowAnonymous]
public async Task<IActionResult> SesNotification()
{
    var body = await new StreamReader(Request.Body).ReadToEndAsync();
    var snsMessage = JsonSerializer.Deserialize<SnsMessage>(body);

    // Bước 1: Xử lý SubscriptionConfirmation (lần đầu setup)
    if (snsMessage.Type == "SubscriptionConfirmation")
    {
        // Confirm subscription bằng cách GET SubscribeURL
        await _httpClient.GetAsync(snsMessage.SubscribeURL);
        return Ok();
    }

    if (snsMessage.Type != "Notification")
        return Ok();

    // Bước 2: Parse Message (là JSON string lồng trong JSON)
    var notification = JsonSerializer.Deserialize<SesNotification>(snsMessage.Message);

    switch (notification.NotificationType)
    {
        case "Bounce":
            await HandleBounce(notification.Bounce);
            break;

        case "Complaint":
            await HandleComplaint(notification.Complaint);
            break;
    }

    return Ok();
}

private async Task HandleBounce(BounceNotification bounce)
{
    foreach (var recipient in bounce.BouncedRecipients)
    {
        if (bounce.BounceType == "Permanent")
        {
            // Permanent bounce: đánh dấu email invalid, không gửi nữa
            await _emailBlacklistService.AddAsync(
                recipient.EmailAddress,
                reason: $"Permanent bounce: {recipient.DiagnosticCode}"
            );
        }
        // Transient bounce: có thể retry sau, chỉ log
        _logger.LogWarning("Transient bounce: {Email} - {Subtype}",
            recipient.EmailAddress, bounce.BounceSubType);
    }
}

private async Task HandleComplaint(ComplaintNotification complaint)
{
    foreach (var recipient in complaint.ComplainedRecipients)
    {
        // Unsubscribe ngay lập tức
        await _emailBlacklistService.AddAsync(
            recipient.EmailAddress,
            reason: $"Complaint: {complaint.ComplaintFeedbackType}"
        );
    }
}
```

---

## Suppression List

SES có danh sách "suppression" riêng — email bị bounce/complaint được tự động thêm vào. Khi gửi đến địa chỉ trong danh sách này, SES không thực sự gửi mà vẫn báo thành công (để bảo vệ reputation).

```csharp
// Thêm vào suppression list thủ công
await _sesClient.PutSuppressedDestinationAsync(new PutSuppressedDestinationRequest
{
    EmailAddress = "bad@example.com",
    Reason       = SuppressionListReason.BOUNCE
});

// Kiểm tra
var result = await _sesClient.GetSuppressedDestinationAsync(
    new GetSuppressedDestinationRequest { EmailAddress = "bad@example.com" }
);
```
