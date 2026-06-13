# AWS SES — Tổng quan

## SES là gì?

Amazon Simple Email Service (SES) là dịch vụ gửi email quy mô lớn, chi phí thấp (~$0.10/1000 email). Với Lumina Tutors, SES dùng để gửi: thông báo học phí, xác nhận đăng ký, thông báo lịch học, báo cáo điểm số, OTP.

---

## Môi trường

| Môi trường | Endpoint |
|-----------|---------|
| Sandbox | Chỉ gửi được đến địa chỉ đã verify. Giới hạn 200 email/ngày |
| Production | Gửi được tất cả, phải request AWS để thoát Sandbox |

---

## Cách gửi email với SES

| Phương thức | Dùng khi |
|------------|---------|
| **SES API** (HTTP) | Gửi qua REST API trực tiếp |
| **SMTP** | Tích hợp với ứng dụng dùng SMTP chuẩn |
| **AWS SDK** | Khuyến nghị cho .NET — đơn giản nhất |

---

## Cài đặt

```bash
dotnet add package AWSSDK.SimpleEmail
# hoặc SESv2 (phiên bản mới hơn)
dotnet add package AWSSDK.SimpleEmailV2
```

```json
// appsettings.json
{
  "AWS": {
    "Region": "ap-southeast-1",
    "SES": {
      "FromEmail": "no-reply@lumina.vn",
      "FromName": "Lumina Tutors"
    }
  }
}
```

---

## Các bước setup bắt buộc

1. **Verify domain** (hoặc email address) trong SES Console
2. Cấu hình **DKIM** để tránh spam
3. Cấu hình **SPF** record trong DNS
4. Thiết lập **SNS topic** để nhận bounce/complaint webhook
5. Request **production access** khi sẵn sàng go-live

---

## Danh sách tài liệu

| File | Nội dung |
|------|----------|
| `02-send-email.md` | Gửi email đơn giản và email HTML |
| `03-send-raw-email.md` | Gửi email với attachment |
| `04-bounce-complaint-webhook.md` | Xử lý bounce & complaint qua SNS |
| `05-smtp-config.md` | Cấu hình SMTP credentials |
