# VNPay — Query Giao Dịch (QueryDr)

## Mục đích

API này cho phép merchant **chủ động hỏi VNPay** về kết quả của một giao dịch. Dùng khi:
- Không nhận được IPN (server down, timeout)
- Cần đối soát giao dịch
- Kiểm tra trạng thái giao dịch đang pending

---

## Endpoint

```
POST https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
Content-Type: application/json
```

---

## Request Body

```json
{
  "vnp_RequestId": "REQ20240612143000001",
  "vnp_Version": "2.1.0",
  "vnp_Command": "querydr",
  "vnp_TmnCode": "ABCD1234",
  "vnp_TxnRef": "ORD20240612001",
  "vnp_OrderInfo": "Truy van giao dich ORD20240612001",
  "vnp_TransactionDate": "20240612143000",
  "vnp_CreateDate": "20240612150000",
  "vnp_IpAddr": "192.168.1.1",
  "vnp_SecureHash": "9f86d081884c7d659a2feaa0c55ad015..."
}
```

### Giải thích từng field

| Field | Bắt buộc | Kiểu | Mô tả |
|-------|----------|------|-------|
| `vnp_RequestId` | ✅ | String[1,32] | ID duy nhất của request này (không trùng trong ngày). Dùng để chống duplicate. |
| `vnp_Version` | ✅ | String | Luôn `2.1.0` |
| `vnp_Command` | ✅ | String | Luôn `querydr` |
| `vnp_TmnCode` | ✅ | String(8) | Mã merchant |
| `vnp_TxnRef` | ✅ | String[1,100] | Mã giao dịch của merchant cần truy vấn |
| `vnp_OrderInfo` | ✅ | String[1,255] | Mô tả lý do truy vấn |
| `vnp_TransactionNo` | ❌ | Numeric | Mã GD phía VNPay (nếu có — tùy chọn) |
| `vnp_TransactionDate` | ✅ | Numeric(14) | Thời gian tạo đơn ban đầu `yyyyMMddHHmmss` (giống `vnp_CreateDate` lúc tạo) |
| `vnp_CreateDate` | ✅ | Numeric(14) | Thời điểm tạo request **truy vấn này** `yyyyMMddHHmmss` |
| `vnp_IpAddr` | ✅ | String | IP server merchant gọi API |
| `vnp_SecureHash` | ✅ | String | HMAC-SHA512 — xem cách tạo bên dưới |

### Cách tạo SecureHash cho QueryDr

```
data = vnp_RequestId + "|" + vnp_Version + "|" + vnp_Command + "|" + vnp_TmnCode
     + "|" + vnp_TxnRef + "|" + vnp_TransactionDate + "|" + vnp_CreateDate
     + "|" + vnp_IpAddr + "|" + vnp_OrderInfo

secureHash = HMAC_SHA512(secretKey, data)
```

> Lưu ý: QueryDr dùng `|` (pipe) nối dữ liệu, **khác** với cách tạo hash lúc tạo URL thanh toán (dùng `&key=value`).

---

## Response từ VNPay

```json
{
  "vnp_ResponseId": "RESP20240612150001",
  "vnp_Command": "querydr",
  "vnp_TmnCode": "ABCD1234",
  "vnp_TxnRef": "ORD20240612001",
  "vnp_Amount": "10000000",
  "vnp_OrderInfo": "Thanh toan hoc phi thang 6",
  "vnp_ResponseCode": "00",
  "vnp_Message": "Giao dich thanh cong",
  "vnp_BankCode": "NCB",
  "vnp_PayDate": "20240612143512",
  "vnp_TransactionNo": "14509170",
  "vnp_TransactionType": "01",
  "vnp_TransactionStatus": "00",
  "vnp_SecureHash": "abc123..."
}
```

### Giải thích Response

| Field | Mô tả |
|-------|-------|
| `vnp_ResponseId` | ID response duy nhất phía VNPay |
| `vnp_ResponseCode` | `00` = query thành công (không phải GD thành công) |
| `vnp_Message` | Mô tả `vnp_ResponseCode` |
| `vnp_TransactionType` | `01` = thanh toán, `02` = hoàn toàn phần, `03` = hoàn một phần |
| `vnp_TransactionStatus` | Trạng thái thực của giao dịch (xem bảng bên dưới) |
| `vnp_Amount` | Số tiền × 100 |
| `vnp_PayDate` | Thời điểm thanh toán tại VNPay |
| `vnp_BankCode` | Ngân hàng/ví thực hiện |
| `vnp_PromotionCode` | Mã QR khuyến mại (nếu có) |
| `vnp_PromotionAmount` | Số tiền giảm giá (nếu có) |
| `vnp_SecureHash` | Verify bằng cách tạo lại hash và so sánh |

### Bảng vnp_TransactionStatus

| Code | Trạng thái |
|------|-----------|
| `00` | Giao dịch thành công |
| `01` | Giao dịch chưa hoàn tất |
| `02` | Giao dịch lỗi |
| `04` | Giao dịch đảo (khách bị trừ tiền nhưng VNPay chưa nhận) |
| `05` | VNPay đang xử lý hoàn tiền |
| `06` | VNPay đã gửi yêu cầu hoàn tiền sang ngân hàng |
| `07` | Giao dịch bị nghi ngờ gian lận |
| `09` | Hoàn trả bị từ chối |

---

## Code mẫu C#

```csharp
public async Task<QueryDrResponse> QueryTransaction(string txnRef, string transDate)
{
    var requestId = Guid.NewGuid().ToString("N")[..20];
    var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

    var data = $"{requestId}|2.1.0|querydr|{_tmnCode}|{txnRef}|{transDate}|{createDate}|{_serverIp}|Truy van GD {txnRef}";
    var secureHash = ComputeHmacSha512(_hashSecret, data);

    var body = new
    {
        vnp_RequestId     = requestId,
        vnp_Version       = "2.1.0",
        vnp_Command       = "querydr",
        vnp_TmnCode       = _tmnCode,
        vnp_TxnRef        = txnRef,
        vnp_OrderInfo     = $"Truy van GD {txnRef}",
        vnp_TransactionDate = transDate,
        vnp_CreateDate    = createDate,
        vnp_IpAddr        = _serverIp,
        vnp_SecureHash    = secureHash
    };

    var response = await _httpClient.PostAsJsonAsync(
        "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction", body);

    return await response.Content.ReadFromJsonAsync<QueryDrResponse>();
}
```
