# Báo cáo đồ án môn học Thương mại điện tử — Lumina Tutors

## Nội dung thư mục

| Tệp / thư mục | Mô tả |
|---|---|
| `BÁO CÁO - Thương mại điện tử - Lumina Tutors.docx` | **File nộp chính.** 42 trang thân bài + 7 trang phần đầu. |
| `BÁO CÁO - Thương mại điện tử - Lumina Tutors (xem trước).pdf` | Bản PDF để xem nhanh bố cục (đã cập nhật mục lục). |
| `hinh-anh/` | 7 sơ đồ PNG đã chèn sẵn vào báo cáo. |
| `nguon/sinh_so_do.py` | Script sinh lại 7 sơ đồ (Graphviz), có kiểm tra cỡ chữ hiệu dụng. |
| `nguon/noi_dung.py` | Toàn bộ nội dung chữ của báo cáo — **sửa nội dung ở đây**. |
| `nguon/dung_bao_cao.py` | Script dựng lại file Word từ mẫu gốc. |
| `nguon/mau_goc.docx` | Mẫu báo cáo gốc của giảng viên (giữ nguyên để đối chiếu). |

## Việc cần làm sau khi mở file

1. **Cập nhật mục lục và danh mục.** File đã bật cờ tự cập nhật khi mở. Nếu Word không hỏi,
   nhấn `Ctrl+A` rồi `F9`, chọn *Update entire table* cho từng mục lục.
2. **Chèn 6 ảnh chụp màn hình.** Trong Chương 3 có 6 khung viền nét đứt ghi
   `[ Chèn ảnh chụp màn hình: ... ]`. Nhấp vào khung, xóa dòng chữ và dán ảnh vào.
   Chú thích *Hình 3.x* bên dưới đã có sẵn và tự đánh số lại.
   - Hình 3.4 — Màn hình danh mục gói dịch vụ và bảng giá theo chu kỳ
   - Hình 3.5 — Màn hình danh sách hóa đơn học phí và bộ lọc theo trạng thái
   - Hình 3.8 — Trang giới thiệu công khai và khu vực bảng tin của trường
   - Hình 3.9 — Giao diện hội thoại của phân hệ Gia Sư AI
   - Hình 3.10 — Màn hình quản lý thuê bao và đơn hàng của quản trị nền tảng
   - Hình 3.12 — Trang kết quả giao dịch sau khi thanh toán qua VNPay
3. **Điền ngày trên trang bìa** (`TP.HCM, ngày ___ tháng 07 năm 2026`).
4. **Rà lại Mục 4.4 — Phân công công việc.** Nội dung hiện là bản đề xuất theo phần việc
   mô tả trong báo cáo; chỉnh lại cho khớp thực tế của nhóm.

## Định dạng đã áp dụng

- Khổ A4, lề trái 3 cm, phải 2 cm, trên/dưới 2,5 cm.
- Times New Roman 13 pt, giãn dòng 1,5, canh đều, thụt đầu dòng 1 cm.
- Tiêu đề: cấp 1 in đậm 14 pt canh giữa, cấp 2 in đậm 13 pt, cấp 3 in đậm nghiêng 13 pt.
- Đánh số trang: `i, ii, iii…` cho phần đầu — `1, 2, 3…` từ Chương 1.
- Chú thích bảng đặt phía trên bảng, chú thích hình đặt phía dưới hình, cỡ 12 pt nghiêng,
  đánh số tự động bằng trường `SEQ` theo từng chương.
- Mục lục, Danh mục bảng, Danh mục hình đều là trường tự động — không gõ tay.

## Lưu ý về nội dung

Báo cáo chỉ mô tả những chức năng **đã cài đặt thật** trong mã nguồn Lumina Tutors.
Các hạng mục chưa triển khai (MoMo/ZaloPay, hóa đơn điện tử có chữ ký số, SEO ngoài trang,
CI/CD, auto scaling, đa ngôn ngữ) được nêu trung thực ở Mục 3.6, Mục 4.2 và đề xuất lộ
trình ở Mục 4.3.

Số liệu thống kê ở Bảng 4.1 được đếm trực tiếp từ mã nguồn tại thời điểm 29/07/2026.
Bảng giá gói dịch vụ (Bảng 3.2, 3.3) lấy từ dữ liệu khởi tạo trong `DatabaseSeeder`.

## Dựng lại file

```bash
cd nguon
python3 sinh_so_do.py      # sinh lại 7 sơ đồ
python3 dung_bao_cao.py    # dựng lại file .docx từ mẫu gốc
```

Cần: `python-docx`, `Pillow`, `graphviz` (lệnh `dot`).
