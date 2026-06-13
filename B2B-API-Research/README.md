# B2B API Research — Lumina Tutors

Tài liệu nghiên cứu REST API cho tích hợp B2B tự động hóa của hệ thống Lumina Tutors.

---

## Cấu trúc thư mục

```
B2B-API-Research/
├── E-Payment/
│   └── VNPay/
│       ├── 01-overview.md          — Tổng quan, môi trường, SecureHash
│       ├── 02-create-payment.md    — Tạo URL thanh toán (redirect)
│       ├── 03-ipn-webhook.md       — IPN server-to-server webhook
│       ├── 04-return-url.md        — ReturnURL hiển thị kết quả UI
│       ├── 05-query-transaction.md — Truy vấn giao dịch (QueryDr)
│       ├── 06-refund.md            — Hoàn tiền (Refund)
│       └── 07-error-codes.md       — Bảng mã lỗi đầy đủ
│
├── Cloud/
│   ├── AWS-S3/
│   │   ├── 01-overview.md          — Khái niệm, cài đặt SDK
│   │   ├── 02-upload-object.md     — Upload file (PutObject)
│   │   ├── 03-get-download-object.md — Download, list file
│   │   ├── 04-presigned-url.md     — URL tạm thời upload/download
│   │   └── 05-delete-and-policy.md — Xóa file, bucket policy, IAM
│   │
│   └── AWS-SES/
│       ├── 01-overview.md          — Giới thiệu, giới hạn, cài đặt
│       ├── 02-send-email.md        — Gửi email HTML/text
│       ├── 03-bounce-complaint-webhook.md — Xử lý bounce/complaint qua SNS
│       └── 04-smtp-config.md       — SMTP credentials & MailKit
│
└── E-Delivery/
    └── README.md                   — Giải thích tại sao không cần cho Lumina
```

---

## Tóm tắt nhanh

### VNPay — E-Payment

| API | Endpoint | Method | Mô tả |
|-----|----------|--------|-------|
| Tạo thanh toán | `vpcpay.html?{params}` | Redirect | Tạo URL redirect người dùng sang trang TT |
| IPN Webhook | `/payment/ipn` (phía bạn) | POST (từ VNPay) | VNPay notify server-to-server |
| ReturnURL | `/payment/return` (phía bạn) | GET (redirect) | Browser redirect về sau TT |
| Query giao dịch | `/merchant_webapi/api/transaction` | POST JSON | Truy vấn trạng thái GD |
| Hoàn tiền | `/merchant_webapi/api/transaction` | POST JSON | Yêu cầu refund |

**Sandbox:** `sandbox.vnpayment.vn` | **Production:** `pay.vnpay.vn`

### AWS S3 — File Storage

| Thao tác | SDK Method | REST Verb |
|----------|-----------|-----------|
| Upload | `PutObjectAsync` | PUT |
| Download | `GetObjectAsync` | GET |
| Xóa | `DeleteObjectAsync` | DELETE |
| Presigned Upload URL | `GetPreSignedURL (PUT)` | — |
| Presigned Download URL | `GetPreSignedURL (GET)` | — |

**Region khuyến nghị:** `ap-southeast-1` (Singapore)

### AWS SES — Email

| Thao tác | SDK Method | REST Verb |
|----------|-----------|-----------|
| Gửi email | `SendEmailAsync` | POST `/v2/email/outbound-emails` |
| Xử lý bounce | SNS Webhook | POST (từ SNS) |
| Xử lý complaint | SNS Webhook | POST (từ SNS) |
| SMTP | MailKit / FluentEmail | TCP 587 |

**Endpoint SMTP:** `email-smtp.ap-southeast-1.amazonaws.com:587`

---

## Môi trường & Credentials

```json
// appsettings.Development.json
{
  "VnPay": {
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ApiUrl": "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction",
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "ReturnUrl": "https://localhost:60480/payment/return",
    "IpnUrl": "https://your-ngrok-url/payment/ipn"
  },
  "AWS": {
    "Region": "ap-southeast-1",
    "BucketName": "lumina-tutors-dev",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "SES": {
      "FromEmail": "no-reply@lumina.vn",
      "FromName": "Lumina Tutors",
      "SmtpUsername": "YOUR_SMTP_USERNAME",
      "SmtpPassword": "YOUR_SMTP_PASSWORD"
    }
  }
}
```

> **Không commit credentials lên Git.** Dùng `dotnet user-secrets` cho dev, AWS Secrets Manager cho production.

---

## Nguồn tài liệu gốc

- VNPay: https://sandbox.vnpayment.vn/apis/
- AWS S3: https://docs.aws.amazon.com/AmazonS3/latest/API/
- AWS SES: https://docs.aws.amazon.com/ses/latest/APIReference/
- AWS SDK .NET: https://docs.aws.amazon.com/sdk-for-net/
