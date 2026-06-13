# VNPay — ReturnURL (Redirect sau thanh toán)

## ReturnURL là gì?

Sau khi người dùng thanh toán xong (hoặc hủy) tại trang VNPay, browser sẽ **redirect** về `vnp_ReturnUrl` với kết quả kèm theo query string. Đây là luồng **hiển thị UI** cho người dùng.

```
VNPay Page ──redirect browser──► https://lumina.vn/payment/return?vnp_*=...
```

> **Quan trọng:** Không dùng ReturnURL để cập nhật DB chính. Người dùng có thể tắt browser trước khi redirect xảy ra. Hãy dùng IPN cho logic nghiệp vụ.

---

## Tham số VNPay gửi về ReturnURL

Giống hệt IPN, VNPay append các tham số vào query string:

| Tham số | Kiểu | Mô tả |
|---------|------|-------|
| `vnp_TmnCode` | String(8) | Mã merchant |
| `vnp_Amount` | Numeric | Số tiền × 100 |
| `vnp_BankCode` | String | Mã ngân hàng/ví |
| `vnp_BankTranNo` | String | Mã GD tại ngân hàng |
| `vnp_CardType` | String | Loại thẻ: `ATM` / `QRCODE` / `VISA` |
| `vnp_PayDate` | Numeric(14) | `yyyyMMddHHmmss` |
| `vnp_OrderInfo` | String | Mô tả đơn hàng |
| `vnp_TransactionNo` | Numeric | Mã GD phía VNPay |
| `vnp_ResponseCode` | String(2) | **`00`** = thành công |
| `vnp_TransactionStatus` | String(2) | **`00`** = hoàn thành |
| `vnp_TxnRef` | String | Mã GD của merchant |
| `vnp_SecureHash` | String | Checksum để verify |

---

## Ví dụ URL ReturnURL nhận được

```
https://lumina.vn/payment/return
  ?vnp_Amount=10000000
  &vnp_BankCode=NCB
  &vnp_BankTranNo=VNP14509170
  &vnp_CardType=ATM
  &vnp_OrderInfo=Thanh+toan+hoc+phi+thang+6
  &vnp_PayDate=20240612143512
  &vnp_ResponseCode=00
  &vnp_TmnCode=ABCD1234
  &vnp_TransactionNo=14509170
  &vnp_TransactionStatus=00
  &vnp_TxnRef=ORD20240612001
  &vnp_SecureHash=9f86d081...
```

---

## Xử lý ReturnURL — Code mẫu C#

```csharp
[HttpGet("return")]
public async Task<IActionResult> PaymentReturn()
{
    var vnpayData = Request.Query
        .ToDictionary(k => k.Key, v => v.Value.ToString());

    // 1. Verify SecureHash
    var secureHash = vnpayData["vnp_SecureHash"];
    vnpayData.Remove("vnp_SecureHash");
    vnpayData.Remove("vnp_SecureHashType");

    var signData = string.Join("&",
        vnpayData.OrderBy(k => k.Key)
                 .Select(k => $"{k.Key}={k.Value}"));
    var computedHash = ComputeHmacSha512(_hashSecret, signData);

    if (!computedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase))
    {
        // Dữ liệu bị giả mạo
        return RedirectToAction("PaymentFailed", new { reason = "invalid_hash" });
    }

    var txnRef     = vnpayData["vnp_TxnRef"];
    var responseCode = vnpayData["vnp_ResponseCode"];
    var txnStatus    = vnpayData["vnp_TransactionStatus"];

    // 2. Kiểm tra kết quả và hiển thị UI
    if (responseCode == "00" && txnStatus == "00")
    {
        // Thanh toán thành công — không cập nhật DB ở đây (đã có IPN)
        // Chỉ hiển thị màn hình thành công cho người dùng
        var order = await _orderService.GetByTxnRef(txnRef);
        return View("PaymentSuccess", new PaymentResultViewModel
        {
            OrderId      = txnRef,
            Amount       = long.Parse(vnpayData["vnp_Amount"]) / 100,
            BankCode     = vnpayData["vnp_BankCode"],
            TransactionNo = vnpayData["vnp_TransactionNo"],
            PayDate      = vnpayData["vnp_PayDate"]
        });
    }
    else
    {
        // Thanh toán thất bại hoặc người dùng hủy
        return View("PaymentFailed", new PaymentResultViewModel
        {
            OrderId      = txnRef,
            ResponseCode = responseCode,
            Message      = GetErrorMessage(responseCode)
        });
    }
}
```

---

## Mã lỗi vnp_ResponseCode phổ biến

| Code | Ý nghĩa |
|------|---------|
| `00` | Thanh toán thành công |
| `07` | Trừ tiền thành công nhưng giao dịch bị nghi ngờ gian lận |
| `09` | Thẻ/tài khoản chưa đăng ký dịch vụ Internet Banking |
| `10` | Xác thực thẻ/tài khoản quá 3 lần |
| `11` | Giao dịch hết hạn chờ thanh toán |
| `12` | Thẻ/tài khoản bị khóa |
| `13` | OTP nhập sai |
| `24` | Người dùng hủy giao dịch |
| `51` | Tài khoản không đủ số dư |
| `65` | Vượt hạn mức giao dịch trong ngày |
| `75` | Ngân hàng đang bảo trì |
| `79` | Nhập sai mật khẩu thanh toán quá số lần quy định |
| `99` | Lỗi khác |

---

## ReturnURL vs IPN — Nên dùng cái nào?

| | ReturnURL | IPN |
|--|-----------|-----|
| Trigger | Browser redirect (người dùng) | Server-to-server (VNPay chủ động) |
| Độ tin cậy | Thấp — người dùng có thể tắt browser | Cao — luôn được gọi |
| Dùng để | Hiển thị UI kết quả | Cập nhật DB, xử lý nghiệp vụ |
| Retry nếu thất bại | Không | Có |

**Kết luận:** Cập nhật DB trong **IPN**, hiển thị UI trong **ReturnURL**.
