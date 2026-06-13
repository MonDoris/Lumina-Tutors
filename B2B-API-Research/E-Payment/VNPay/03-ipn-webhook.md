# VNPay — IPN Webhook (Instant Payment Notification)

## IPN là gì?

IPN là cơ chế **server-to-server** — VNPay chủ động gọi HTTP đến `vnp_IpnUrl` của merchant để thông báo kết quả giao dịch, **không qua browser**. Đây là cách đáng tin cậy nhất để cập nhật trạng thái đơn hàng.

```
VNPay Server ──POST──► https://lumina.vn/payment/ipn
```

---

## Endpoint phía Merchant

```
POST https://lumina.vn/payment/ipn
Content-Type: application/x-www-form-urlencoded
```

> VNPay gọi bằng **HTTP GET hoặc POST** tùy cấu hình. Nên hỗ trợ cả hai.

---

## Tham số VNPay gửi đến IPN URL

| Tham số | Kiểu | Mô tả |
|---------|------|-------|
| `vnp_TmnCode` | String(8) | Mã merchant |
| `vnp_Amount` | Numeric | Số tiền × 100 |
| `vnp_BankCode` | String | Mã ngân hàng/ví thanh toán |
| `vnp_BankTranNo` | String | Mã giao dịch tại ngân hàng |
| `vnp_CardType` | String | Loại thẻ: `ATM` / `QRCODE` / `VISA` |
| `vnp_PayDate` | Numeric(14) | Thời gian thanh toán `yyyyMMddHHmmss` |
| `vnp_OrderInfo` | String | Mô tả đơn hàng (đã gửi lúc tạo) |
| `vnp_TransactionNo` | Numeric | Mã giao dịch phía VNPay |
| `vnp_ResponseCode` | String(2) | Kết quả thanh toán (**`00`** = thành công) |
| `vnp_TransactionStatus` | String(2) | Trạng thái giao dịch (**`00`** = hoàn thành) |
| `vnp_TxnRef` | String | Mã giao dịch của merchant (gửi lúc tạo) |
| `vnp_SecureHash` | String | Checksum HMAC-SHA512 để verify |

---

## Ví dụ dữ liệu IPN nhận được

```
vnp_Amount=10000000
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
&vnp_SecureHash=9f86d081884c7d659a2feaa0c55ad015...
```

---

## Response trả về cho VNPay

Merchant phải trả về JSON để VNPay biết đã nhận thành công:

```json
{ "RspCode": "00", "Message": "Confirm Success" }
```

**Các mã response:**

| RspCode | Ý nghĩa | Khi nào dùng |
|---------|---------|--------------|
| `00` | Xác nhận thành công | Đã xử lý đơn hàng thành công |
| `01` | Order not found | Không tìm thấy `vnp_TxnRef` trong DB |
| `02` | Order already confirmed | Đơn hàng đã được xác nhận trước đó |
| `04` | Invalid amount | Số tiền không khớp |
| `97` | Invalid checksum | `vnp_SecureHash` không hợp lệ |
| `99` | Unknown error | Lỗi khác |

> **Nếu trả `RspCode != 00`**, VNPay sẽ **retry** lại IPN sau một khoảng thời gian.

---

## Xử lý IPN — Thuật toán chuẩn

```csharp
[HttpGet("ipn")]
public IActionResult IPN()
{
    // 1. Lấy toàn bộ query params từ request
    var vnpayData = Request.Query
        .ToDictionary(k => k.Key, v => v.Value.ToString());

    // 2. Lấy và xóa SecureHash khỏi tập params
    var vnpSecureHash = vnpayData["vnp_SecureHash"];
    vnpayData.Remove("vnp_SecureHash");
    vnpayData.Remove("vnp_SecureHashType");

    // 3. Tạo lại hash từ dữ liệu nhận được
    var signData = string.Join("&",
        vnpayData.OrderBy(k => k.Key)
                 .Select(k => $"{k.Key}={k.Value}"));

    var computedHash = ComputeHmacSha512(_hashSecret, signData);

    // 4. So sánh hash — nếu khác => từ chối
    if (!computedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase))
        return Ok(new { RspCode = "97", Message = "Invalid checksum" });

    // 5. Kiểm tra đơn hàng tồn tại
    var txnRef = vnpayData["vnp_TxnRef"];
    var order = await _orderService.GetByTxnRef(txnRef);
    if (order == null)
        return Ok(new { RspCode = "01", Message = "Order not found" });

    // 6. Kiểm tra đã xử lý chưa (tránh duplicate)
    if (order.Status == OrderStatus.Paid)
        return Ok(new { RspCode = "02", Message = "Order already confirmed" });

    // 7. Kiểm tra số tiền
    var amount = long.Parse(vnpayData["vnp_Amount"]);
    if (amount != order.Amount * 100)
        return Ok(new { RspCode = "04", Message = "Invalid amount" });

    // 8. Kiểm tra kết quả thanh toán
    if (vnpayData["vnp_ResponseCode"] == "00" &&
        vnpayData["vnp_TransactionStatus"] == "00")
    {
        await _orderService.MarkAsPaid(txnRef, vnpayData["vnp_TransactionNo"]);
    }

    return Ok(new { RspCode = "00", Message = "Confirm Success" });
}
```

---

## Lưu ý triển khai

- IPN URL phải **public** (không cần đăng nhập), nhưng phải xác thực bằng SecureHash
- Server phải trả response trong **vòng 5 giây**, nếu không VNPay coi là timeout và retry
- Phải xử lý **idempotency** — cùng một giao dịch có thể gọi IPN nhiều lần
- Kiểm tra cả `vnp_ResponseCode == "00"` **VÀ** `vnp_TransactionStatus == "00"` mới coi là thành công
- Log toàn bộ raw request vào DB để tra cứu khi có tranh chấp
