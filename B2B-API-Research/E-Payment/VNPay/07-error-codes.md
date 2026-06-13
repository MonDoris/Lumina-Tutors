# VNPay — Bảng Mã Lỗi Đầy Đủ

## vnp_ResponseCode — Kết quả thanh toán (ReturnURL / IPN)

| Code | Ý nghĩa | Hành động |
|------|---------|-----------|
| `00` | Giao dịch thành công | Cập nhật đơn hàng thành công |
| `07` | Trừ tiền thành công, GD bị nghi gian lận | Chờ VNPay xác nhận thêm |
| `09` | Chưa đăng ký Internet Banking | Thông báo người dùng |
| `10` | Xác thực thẻ sai quá 3 lần | Thông báo người dùng |
| `11` | Hết hạn chờ thanh toán | Tạo lại đơn mới |
| `12` | Thẻ/tài khoản bị khóa | Thông báo người dùng |
| `13` | Sai OTP | Thông báo người dùng |
| `24` | Người dùng hủy giao dịch | Không làm gì, hiển thị UI hủy |
| `51` | Không đủ số dư | Thông báo người dùng |
| `65` | Vượt hạn mức giao dịch ngày | Thông báo người dùng |
| `75` | Ngân hàng bảo trì | Thử lại sau |
| `79` | Sai mật khẩu thanh toán quá lần cho phép | Thông báo người dùng |
| `99` | Lỗi khác | Log + liên hệ VNPay |

## vnp_TransactionStatus — Trạng thái giao dịch

| Code | Trạng thái |
|------|-----------|
| `00` | Thành công |
| `01` | Chưa hoàn tất |
| `02` | Lỗi |
| `04` | Giao dịch đảo |
| `05` | VNPay đang xử lý hoàn |
| `06` | Đã gửi yêu cầu hoàn sang ngân hàng |
| `07` | Nghi ngờ gian lận |
| `09` | Hoàn trả bị từ chối |

## vnp_ResponseCode — QueryDr

| Code | Ý nghĩa |
|------|---------|
| `00` | Query thành công |
| `02` | TmnCode không hợp lệ |
| `03` | Dữ liệu sai định dạng |
| `91` | Không tìm thấy giao dịch |
| `94` | Request trùng lặp |
| `97` | Checksum không hợp lệ |
| `99` | Lỗi khác |

## vnp_ResponseCode — Refund

| Code | Ý nghĩa |
|------|---------|
| `00` | Yêu cầu hoàn thành công |
| `02` | TmnCode không hợp lệ |
| `03` | Dữ liệu sai định dạng |
| `91` | Không tìm thấy GD cần hoàn |
| `94` | Đã có yêu cầu hoàn đang xử lý |
| `95` | GD gốc không thành công |
| `97` | Checksum không hợp lệ |
| `99` | Lỗi khác |

## Quy tắc xử lý mã lỗi

```
GD thành công khi: vnp_ResponseCode == "00" AND vnp_TransactionStatus == "00"
GD hủy khi:       vnp_ResponseCode == "24"
GD thất bại:      vnp_ResponseCode != "00" (mọi trường hợp còn lại)
```
