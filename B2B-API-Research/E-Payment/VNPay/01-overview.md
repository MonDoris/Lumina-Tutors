# VNPay API — Tổng quan

## Giới thiệu

VNPay là cổng thanh toán trực tuyến phổ biến nhất Việt Nam. Merchant (website của bạn) tích hợp VNPay bằng cách chuyển hướng người dùng sang trang thanh toán VNPay, sau đó VNPay gọi lại (IPN/ReturnURL) để thông báo kết quả.

**Tài liệu chính thức:** https://sandbox.vnpayment.vn/apis/

---

## Môi trường

| Môi trường  | Base URL                                              |
|-------------|-------------------------------------------------------|
| Sandbox     | `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`  |
| Production  | `https://pay.vnpay.vn/vpcpay.html`                    |
| API (Query/Refund) | `https://sandbox.vnpayment.vn/merchant_webapi/api/transaction` |

---

## Thông tin cấu hình cần có

| Tham số        | Ý nghĩa                                                              |
|----------------|----------------------------------------------------------------------|
| `vnp_TmnCode`  | Mã định danh merchant (8 ký tự), do VNPay cấp khi đăng ký           |
| `vnp_HashSecret` | Khóa bí mật dùng để tạo và xác thực `SecureHash` (HMAC-SHA512)   |
| `vnp_ReturnUrl` | URL trên server merchant — VNPay redirect người dùng về đây sau TT |
| `vnp_IpnUrl`   | URL trên server merchant — VNPay POST kết quả giao dịch về đây      |

---

## Luồng hoạt động tổng quan

```
[Người dùng] ──click Thanh toán──► [Merchant Server]
                                         │
                              Tạo URL + SecureHash
                                         │
                                    Redirect ──► [VNPay Payment Page]
                                                      │
                                         Người dùng nhập thẻ/QR
                                                      │
                          ┌───────────── Kết quả ─────┤
                          │                           │
                    IPN (POST)                   ReturnURL (GET)
                   (Server-to-Server)           (Browser redirect)
                          │                           │
                  Merchant cập nhật DB         Merchant hiển thị kết quả
```

**Lưu ý quan trọng:**
- **IPN (Instant Payment Notification)** là luồng chính xác nhất — gọi trực tiếp server-to-server, không qua browser. Dùng IPN để cập nhật DB.
- **ReturnURL** chỉ dùng để hiển thị UI, không đảm bảo 100% (người dùng có thể tắt browser).
- Luôn verify `SecureHash` trước khi xử lý bất kỳ callback nào.

---

## Cơ chế SecureHash (HMAC-SHA512)

Mọi request/response đều có `vnp_SecureHash` để chống giả mạo dữ liệu.

**Cách tạo:**
1. Lấy tất cả tham số `vnp_*` (trừ `vnp_SecureHash` và `vnp_SecureHashType`)
2. Sắp xếp theo thứ tự **alphabet** (key)
3. Nối thành chuỗi: `key=value&key=value&...`
4. HMAC-SHA512 với `vnp_HashSecret`

```csharp
// C# example
var sortedParams = requestParams
    .Where(p => p.Key.StartsWith("vnp_") && p.Key != "vnp_SecureHash")
    .OrderBy(p => p.Key)
    .Select(p => $"{p.Key}={p.Value}");

var data = string.Join("&", sortedParams);
var hash = ComputeHmacSha512(vnpHashSecret, data);
```

---

## Phiên bản API

Hiện tại: **`2.1.0`** — luôn truyền trong tham số `vnp_Version`.

---

## Danh sách tài liệu trong thư mục này

| File | Nội dung |
|------|----------|
| `02-create-payment.md` | Tạo URL thanh toán (tham số, ví dụ) |
| `03-ipn-webhook.md` | IPN Webhook — xử lý server-to-server |
| `04-return-url.md` | ReturnURL — xử lý sau khi redirect về |
| `05-query-transaction.md` | Query giao dịch (querydr) |
| `06-refund.md` | Hoàn tiền (refund) |
| `07-error-codes.md` | Bảng mã lỗi đầy đủ |
