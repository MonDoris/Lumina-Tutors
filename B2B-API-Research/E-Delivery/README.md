# E-Delivery — Có cần thiết cho Lumina Tutors không?

## Kết luận ngắn

**Không cần thiết** cho hệ thống quản lý giáo dục như Lumina Tutors trong giai đoạn hiện tại.

---

## Lý do

E-Delivery (GHN, GHTK, ViettelPost...) là API vận chuyển hàng hóa vật lý — đặt đơn giao hàng, tracking bưu kiện, tính phí ship. Các nghiệp vụ này không phát sinh trong mô hình trung tâm đào tạo thuần túy.

Lumina Tutors cung cấp dịch vụ giáo dục (lịch học, điểm số, học phí) — không giao hàng vật lý.

---

## Trường hợp ngoại lệ — Khi nào CÓ thể cần

| Tình huống | API cần |
|-----------|---------|
| Gửi sách giáo khoa, học liệu vật lý đến học viên | GHN / GHTK |
| Gửi giấy chứng nhận, bằng tốt nghiệp qua đường bưu điện | ViettelPost |
| Bán combo khóa học + tài liệu in | GHN / GHTK |

---

## Nếu cần tích hợp trong tương lai

Tài liệu tham khảo:
- **GHN:** https://api.ghn.vn/home/docs/detail
- **GHTK:** https://docs.giaohangtietkiem.vn/
- **ViettelPost:** https://viettelpost.com.vn/

Các API điển hình cần tích hợp:
- `POST /v2/shipping-order/create` — Tạo đơn giao hàng
- `GET /v2/shipping-order/detail` — Lấy chi tiết đơn
- `POST /webhook` — Nhận cập nhật trạng thái (giống IPN của VNPay)
- `GET /v2/shipping-order/tracking` — Tracking hành trình bưu kiện
