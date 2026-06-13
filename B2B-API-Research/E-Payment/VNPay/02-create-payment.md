# VNPay — Tạo URL Thanh toán

## Endpoint

```
GET https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?{params}
```

> **Cách hoạt động:** Không phải gọi API trực tiếp — bạn xây dựng query string rồi **redirect browser** người dùng sang URL này. VNPay hiển thị trang thanh toán của họ.

---

## Tham số Request (Query String)

### Tham số bắt buộc

| Tham số | Kiểu | Độ dài | Mô tả | Ví dụ |
|---------|------|--------|-------|-------|
| `vnp_Version` | String | 1–8 | Phiên bản API | `2.1.0` |
| `vnp_Command` | String | 1–16 | Luôn là `pay` cho thanh toán | `pay` |
| `vnp_TmnCode` | String | 8 | Mã merchant do VNPay cấp | `ABCD1234` |
| `vnp_Amount` | Numeric | 1–12 | Số tiền **× 100** (VND, không dấu thập phân) | `10000000` (= 100.000đ) |
| `vnp_CurrCode` | String | 3 | Đơn vị tiền tệ, hiện chỉ dùng `VND` | `VND` |
| `vnp_TxnRef` | String | 1–100 | Mã giao dịch **duy nhất** của merchant trong ngày | `ORD20240612001` |
| `vnp_OrderInfo` | String | 1–255 | Mô tả đơn hàng (URL-encoded, không dấu đặc biệt) | `Thanh+toan+hoc+phi+thang+6` |
| `vnp_OrderType` | String | 1–20 | Loại hàng hóa theo danh mục VNPay | `billpayment` |
| `vnp_Locale` | String | 2–5 | Ngôn ngữ trang thanh toán: `vn` hoặc `en` | `vn` |
| `vnp_ReturnUrl` | String | 10–255 | URL redirect sau khi thanh toán | `https://lumina.vn/payment/return` |
| `vnp_IpAddr` | String | 7–45 | IP của người dùng | `192.168.1.1` |
| `vnp_CreateDate` | Numeric | 14 | Thời gian tạo đơn (GMT+7): `yyyyMMddHHmmss` | `20240612143000` |
| `vnp_SecureHash` | String | 32–256 | HMAC-SHA512 checksum (xem overview) | `abc123...` |

### Tham số tùy chọn

| Tham số | Kiểu | Mô tả | Ví dụ |
|---------|------|-------|-------|
| `vnp_BankCode` | String | Chỉ định ngân hàng/ví — bỏ trống để hiển thị danh sách | `NCB`, `VNPAYQR` |
| `vnp_ExpireDate` | Numeric(14) | Thời gian hết hạn giao dịch `yyyyMMddHHmmss` | `20240612153000` |
| `vnp_Bill_*` | String | Thông tin hóa đơn (tên, địa chỉ, điện thoại, email) | |

---

## Ví dụ URL hoàn chỉnh

```
https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
  ?vnp_Version=2.1.0
  &vnp_Command=pay
  &vnp_TmnCode=ABCD1234
  &vnp_Amount=10000000
  &vnp_CurrCode=VND
  &vnp_TxnRef=ORD20240612001
  &vnp_OrderInfo=Thanh+toan+hoc+phi+thang+6
  &vnp_OrderType=billpayment
  &vnp_Locale=vn
  &vnp_ReturnUrl=https%3A%2F%2Flumina.vn%2Fpayment%2Freturn
  &vnp_IpAddr=192.168.1.1
  &vnp_CreateDate=20240612143000
  &vnp_SecureHash=9f86d081884c7d659a2feaa0c55ad015...
```

---

## Code mẫu C# (ASP.NET Core)

```csharp
public string CreatePaymentUrl(PaymentRequest model, HttpContext context)
{
    var vnpay = new VnPayLibrary();

    vnpay.AddRequestData("vnp_Version", "2.1.0");
    vnpay.AddRequestData("vnp_Command", "pay");
    vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
    vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
    vnpay.AddRequestData("vnp_CurrCode", "VND");
    vnpay.AddRequestData("vnp_TxnRef", model.OrderId);
    vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan hoc phi {model.Description}");
    vnpay.AddRequestData("vnp_OrderType", "billpayment");
    vnpay.AddRequestData("vnp_Locale", "vn");
    vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:ReturnUrl"]);
    vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString());
    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
    vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

    return vnpay.CreateRequestUrl(
        _config["VnPay:PaymentUrl"],
        _config["VnPay:HashSecret"]
    );
}
```

---

## Lưu ý quan trọng

- `vnp_Amount` = số tiền thực **× 100**. Ví dụ 150.000đ → `15000000`
- `vnp_TxnRef` **không được trùng trong cùng một ngày** (YYYYMMDD). Thực tế nên dùng: `{timestamp}_{orderId}`
- `vnp_OrderInfo` phải URL-encode, không chứa `|` và ký tự đặc biệt không encode
- Nếu `vnp_BankCode` = rỗng → VNPay hiển thị danh sách ngân hàng cho người dùng chọn
- `vnp_OrderType` phổ biến: `billpayment` (hóa đơn), `topup` (nạp tiền), `fashion` (thời trang)
