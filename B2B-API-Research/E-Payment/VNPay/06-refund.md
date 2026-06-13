# VNPay — Hoàn Tiền (Refund)

## Endpoint

```
POST https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
Content-Type: application/json
```

---

## Loại hoàn tiền

| vnp_TransactionType | Ý nghĩa |
|--------------------|---------|
| `02` | Hoàn trả **toàn phần** — hoàn đúng số tiền giao dịch gốc |
| `03` | Hoàn trả **một phần** — hoàn số tiền nhỏ hơn giao dịch gốc |

---

## Request Body

```json
{
  "vnp_RequestId": "REF20240612160000001",
  "vnp_Version": "2.1.0",
  "vnp_Command": "refund",
  "vnp_TmnCode": "ABCD1234",
  "vnp_TransactionType": "02",
  "vnp_TxnRef": "ORD20240612001",
  "vnp_Amount": "10000000",
  "vnp_OrderInfo": "Hoan tien hoc phi thang 6",
  "vnp_TransactionNo": "14509170",
  "vnp_TransactionDate": "20240612143000",
  "vnp_CreateBy": "admin@lumina.vn",
  "vnp_CreateDate": "20240612160000",
  "vnp_IpAddr": "192.168.1.1",
  "vnp_SecureHash": "def456..."
}
```

### Giải thích từng field

| Field | Bắt buộc | Kiểu | Mô tả |
|-------|----------|------|-------|
| `vnp_RequestId` | ✅ | String[1,32] | ID duy nhất cho request hoàn này |
| `vnp_Version` | ✅ | String | `2.1.0` |
| `vnp_Command` | ✅ | String | `refund` |
| `vnp_TmnCode` | ✅ | String(8) | Mã merchant |
| `vnp_TransactionType` | ✅ | String(2) | `02` = toàn phần, `03` = một phần |
| `vnp_TxnRef` | ✅ | String[1,100] | Mã GD gốc cần hoàn |
| `vnp_Amount` | ✅ | Numeric | Số tiền hoàn × 100. Với hoàn toàn phần = số tiền GD gốc |
| `vnp_OrderInfo` | ✅ | String[1,255] | Lý do hoàn |
| `vnp_TransactionNo` | ❌ | Numeric | Mã GD phía VNPay (tùy chọn, có thể lấy từ QueryDr) |
| `vnp_TransactionDate` | ✅ | Numeric(14) | Thời gian tạo GD gốc `yyyyMMddHHmmss` |
| `vnp_CreateBy` | ✅ | String[1,245] | Username/email người thực hiện hoàn tiền |
| `vnp_CreateDate` | ✅ | Numeric(14) | Thời điểm tạo request hoàn này |
| `vnp_IpAddr` | ✅ | String | IP server merchant |
| `vnp_SecureHash` | ✅ | String | HMAC-SHA512 — xem cách tạo |

### Cách tạo SecureHash cho Refund

```
data = vnp_RequestId + "|" + vnp_Version + "|" + vnp_Command + "|" + vnp_TmnCode
     + "|" + vnp_TransactionType + "|" + vnp_TxnRef + "|" + vnp_Amount
     + "|" + vnp_TransactionNo + "|" + vnp_TransactionDate + "|" + vnp_CreateBy
     + "|" + vnp_CreateDate + "|" + vnp_IpAddr + "|" + vnp_OrderInfo

secureHash = HMAC_SHA512(secretKey, data)
```

---

## Response từ VNPay

```json
{
  "vnp_ResponseId": "RESP20240612160001",
  "vnp_Command": "refund",
  "vnp_TmnCode": "ABCD1234",
  "vnp_TxnRef": "ORD20240612001",
  "vnp_Amount": "10000000",
  "vnp_OrderInfo": "Hoan tien hoc phi thang 6",
  "vnp_ResponseCode": "00",
  "vnp_Message": "Yeu cau thanh cong",
  "vnp_BankCode": "NCB",
  "vnp_PayDate": "20240612160512",
  "vnp_TransactionNo": "14509999",
  "vnp_TransactionType": "02",
  "vnp_TransactionStatus": "00",
  "vnp_SecureHash": "xyz789..."
}
```

### Mã lỗi vnp_ResponseCode — Refund

| Code | Ý nghĩa |
|------|---------|
| `00` | Yêu cầu hoàn thành công |
| `02` | TmnCode không hợp lệ |
| `03` | Dữ liệu sai định dạng |
| `91` | Không tìm thấy giao dịch cần hoàn |
| `94` | Đã gửi yêu cầu hoàn trước đó, VNPay đang xử lý |
| `95` | GD gốc không thành công, VNPay từ chối hoàn |
| `97` | Checksum không hợp lệ |
| `99` | Lỗi khác |

---

## Lưu ý quan trọng

- Chỉ hoàn được giao dịch có `vnp_TransactionStatus = 00` (đã thành công)
- Hoàn một phần (`type=03`) chỉ thực hiện được **1 lần**. Muốn hoàn thêm phải liên hệ VNPay trực tiếp.
- `vnp_RequestId` không được trùng trong ngày — dùng timestamp + random để tạo
- Response `00` chỉ nghĩa là **yêu cầu hoàn được tiếp nhận**, không phải tiền đã hoàn. Cần `vnp_TransactionStatus = 00` để xác nhận tiền đã trả về khách.
