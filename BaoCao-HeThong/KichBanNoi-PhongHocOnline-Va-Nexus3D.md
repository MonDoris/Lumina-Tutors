# KỊCH BẢN NÓI HOÀN CHỈNH — Phòng học Online & Phòng 3D Lumina Nexus

> Bản văn nói ~8 phút, đọc được nguyên văn. Phần *(nghiêng trong ngoặc)* là ghi chú
> riêng cho bạn — không đọc. Dòng **[DEMO]** là lúc thao tác trên máy.
> Cuối file có mục "rút xuống 5 phút" và bộ hỏi–đáp dự phòng.

---

## 1. MỞ ĐẦU (30 giây)

Kính thưa thầy cô, phần này em xin trình bày tính năng học trực tuyến của hệ thống Lumina Tutors. Hệ thống có hai loại phòng: **phòng học online** dạng video call, và **phòng thí nghiệm 3D** tên là Lumina Nexus.

Cả hai chạy trên cùng một nguyên tắc: **tín hiệu đi một đường, hình ảnh đi một đường khác**. SignalR đóng vai trò *tổng đài viên* — chỉ giới thiệu, kết nối hai bên chứ không truyền video. Còn video thật đi bằng WebRTC. Đây là câu xương sống của toàn bộ phần trình bày ạ.

*(Nếu chỉ nhớ 1 câu, nhớ câu in đậm trên — mọi câu hỏi khó đều quay về nó được.)*

---

## 2. PHÒNG HỌC ONLINE 2D (~3 phút)

### 2.1 — Ý tưởng kiến trúc (45 giây)

Phòng online của em hoạt động như **gọi nhóm trực tiếp**: khi vào phòng, mỗi người nối thẳng với từng người khác, video chạy máy-tới-máy, **không đi qua server**. Ưu điểm là server rất nhẹ — dù cả lớp bật camera, server không phải gánh giây video nào. Nhược điểm là đông người thì mỗi máy phải gánh nhiều kết nối, nên kiến trúc này em chủ đích dành cho lớp nhỏ, khoảng sáu đến tám người bật cam — đúng quy mô lớp kèm của trung tâm.

Quá trình hai máy kết nối giống như **làm quen qua mai mối**: máy A gửi *lời mời* qua tổng đài SignalR, máy B *trả lời*, hai bên trao đổi *địa chỉ nhà* — tức địa chỉ mạng — rồi từ đó nói chuyện trực tiếp, không cần tổng đài nữa.

### 2.2 — Các bước em xây dựng (1 phút 30)

Em làm theo thứ tự **từ trong ra ngoài** — dữ liệu trước, nghiệp vụ giữa, giao diện và realtime sau cùng — đúng tư duy Clean Architecture của đồ án:

**Thứ nhất, thiết kế dữ liệu.** Em xác định năm thứ cần lưu: buổi học — có mã phòng và vòng đời *Đã lên lịch, Đang diễn ra, Kết thúc*; người tham gia — vào lúc nào, rời lúc nào, điểm danh chưa; tin nhắn chat; slide bài giảng; và bản ghi hình. Vì hệ thống nhiều trường dùng chung, mỗi bản ghi đều gắn mã trường để cô lập dữ liệu.

**Thứ hai, cập nhật database** bằng migration theo đúng quy trình của dự án.

**Thứ ba, viết lớp nghiệp vụ.** Tạo phòng thì tự sinh mã ngẫu nhiên dạng bốn-chữ-bốn-số, giống số phòng khách sạn. Học sinh nhập mã thì hệ thống kiểm tra ba điều: mã có tồn tại không, phòng kết thúc chưa, phòng đầy chưa — rồi trả về trọn gói: thông tin phòng, danh sách người, năm mươi tin nhắn gần nhất và slide. Mọi hàm đều trả kết quả kèm thông báo tiếng Việt để hiện thẳng lên màn hình.

**Thứ tư, làm các màn hình web**: danh sách buổi học, tạo phòng, nhập mã, và phòng học chính.

**Thứ năm, dựng kênh realtime.** Mỗi buổi học là một *nhóm* trên server; mọi sự kiện — chat, nét vẽ bảng trắng, lật slide, giơ tay, điểm danh — chỉ là một tin nhắn phát cho cả nhóm. Danh tính người gửi do server đọc từ phiên đăng nhập, trình duyệt không tự khai được, nên không giả mạo được tên người khác.

**Thứ sáu, nối video** theo cơ chế bắt tay em vừa trình bày.

**Thứ bảy, ghi hình**: trình duyệt tự ghi buổi học thành file video, kết thúc thì tải lên server để xem lại.

### 2.3 — Demo (45 giây)

**[DEMO]** Em xin demo: bên này em là giáo viên — tạo phòng, hệ thống sinh mã, bấm *Phát phòng*. Bên trình duyệt thứ hai em đóng vai học sinh — nhập mã, vào phòng, hai bên thấy video của nhau.

Vài điểm mời thầy cô để ý: **chat được lưu database** — em tải lại trang, lịch sử vẫn còn; **bảng trắng và slide đồng bộ tức thì** — giáo viên lật trang, máy học sinh lật theo; và giáo viên **điểm danh ngay trong phòng**, dữ liệu ghi thẳng vào hệ thống điểm danh chung.

---

## 3. PHÒNG 3D LUMINA NEXUS (~3 phút)

### 3.1 — Vì sao phải đổi kiến trúc (45 giây)

Phòng thí nghiệm 3D là bài toán khác hẳn: **một giáo viên phát cho nhiều học sinh**. Nếu giữ cách nối máy-tới-máy, giáo viên phải upload video riêng cho từng em — mạng giáo viên nghẽn ngay.

Nên em chuyển sang mô hình **đài truyền hình**, thuật ngữ chuyên môn gọi là SFU: giáo viên gửi *một* luồng duy nhất lên server, server làm *trạm phát sóng*, sao chép gói tin gửi cho từng học sinh. Điểm mấu chốt: trạm này **chỉ chuyển tiếp gói tin thô, không giải mã** — nên máy chủ chịu tải rất nhẹ. Và em **tự viết trạm chuyển tiếp này bằng C#** với thư viện mã nguồn mở SIPSorcery, không phải thuê media server bên ngoài.

### 3.2 — Các bước xây dựng (45 giây)

Em làm sáu bước: **một**, chọn kiến trúc trạm chuyển tiếp như vừa nêu. **Hai**, làm trang phòng 3D toàn màn hình, gác cổng hai lớp — đúng quyền vào lab và trung tâm phải ở gói dịch vụ có tính năng này; hệ thống còn tự đoán môn giáo viên đang dạy để mở sẵn đúng bộ thí nghiệm. **Ba**, viết trạm chuyển tiếp. **Bốn**, dựng kênh tín hiệu riêng, nhớ trong bộ nhớ server: ai đang ở phòng, đang mở thí nghiệm gì, đã đổ hóa chất nào. **Năm**, dựng cảnh 3D trên trình duyệt bằng Three.js — mô hình mẫu vật tô sáng được, tách rời từng bộ phận, gắn nhãn chú thích; giáo viên xoay mô hình thì gửi góc xoay lên server hai mươi lần mỗi giây, máy học sinh nội suy cho mượt. **Sáu**, đồng bộ thí nghiệm — điểm hay nhất em xin trình bày riêng.

### 3.3 — Điểm nhấn: người vào trễ vẫn thấy đúng hiện trường (45 giây)

Server không lưu *trạng thái* cái cốc thí nghiệm, mà lưu **chuỗi hành động**: đổ nước, đổ axit, thả natri. Học sinh vào trễ, hệ thống phát lại chuỗi đó — giống **vào group chat muộn, kéo lên đọc tin nhắn cũ** là hiểu chuyện gì đã xảy ra. Kết quả: ai vào lúc nào cũng thấy đúng màu dung dịch, đúng kết tủa, đúng phương trình trên bảng.

Ngoài ra phòng 3D có **âm thanh không gian**: học sinh đứng xa giáo viên trong phòng ảo thì nghe nhỏ dần, như ngoài đời.

**[DEMO]** Em mở phòng Nexus vai giáo viên, chọn thí nghiệm Hóa, thả natri vào nước — bên máy học sinh thấy đúng hiệu ứng cháy. Và đây là điểm nhấn: em cho một học sinh nữa vào *sau khi* thí nghiệm đã xảy ra — hiện trường tự khôi phục đúng ạ.

---

## 4. CÔNG NGHỆ SỬ DỤNG — CÓ BÊN THỨ BA, CÓ KEY KHÔNG? (~1 phút 30)

Một câu hỏi tự nhiên: em có thuê dịch vụ video nào không, có API key nào không? Câu trả lời: **tính năng phòng học không dùng API key nào và không thuê dịch vụ video bên thứ ba** — toàn bộ tự xây trên thư viện mã nguồn mở:

**SignalR** của chính Microsoft, nằm sẵn trong ASP.NET Core, lo kênh tín hiệu. **WebRTC** không phải thư viện mà là API *có sẵn trong mọi trình duyệt* — công nghệ mà Google Meet cũng dùng, miễn phí bản chất. **SIPSorcery** là thư viện .NET mã nguồn mở giúp server hiểu WebRTC để làm trạm chuyển tiếp. **Three.js** là engine 3D mã nguồn mở, em chép hẳn vào dự án tự host — không có mạng ngoài vẫn chạy. Ghi hình dùng API có sẵn của trình duyệt. Không cái nào cần đăng ký hay trả phí.

Dịch vụ bên ngoài **duy nhất** em chạm tới là máy chủ STUN công cộng miễn phí của Google. Vai trò của nó cực nhỏ: mỗi máy hỏi nó đúng một câu — *"địa chỉ công khai của tôi trên Internet là gì?"* — để hai máy tìm được nhau. Nó **không nhìn thấy và không trung chuyển bất kỳ giây video nào**.

Thứ duy nhất *sẽ* cần tài khoản khi triển khai thật là **TURN server** — trạm trung chuyển dùng khi học sinh ở mạng 4G không nối thẳng được. Em đã chừa sẵn chỗ cấu hình; có thể tự dựng miễn phí bằng coturn trên VPS hoặc thuê dịch vụ. Demo cùng mạng thì chưa cần.

Về bảo mật: mã phòng chỉ là *lời mời*, không phải lớp bảo vệ — muốn vào vẫn phải **đăng nhập bằng tài khoản của trung tâm**, danh tính do server đọc từ phiên đăng nhập nên không giả mạo được. Phòng 3D gác thêm hai lớp: quyền vào lab và gói dịch vụ premium. Toàn bộ chạy HTTPS — vừa là điều kiện trình duyệt cho mở camera, vừa đảm bảo an toàn; riêng luồng video WebRTC tự mã hóa theo chuẩn DTLS-SRTP có sẵn.

Và câu so sánh chốt: *"Nếu em dùng Zoom SDK hay Agora, em phải đăng ký API key, trả phí theo phút gọi, và video học sinh đi qua máy chủ nước ngoài. Cách em chọn: không mất phí dịch vụ nào, dữ liệu điểm danh, chat, bản ghi nằm lại trong database của trung tâm — đổi lại em phải tự viết phần khó nhất là trạm chuyển tiếp media bằng C#."*

---

## 5. KẾT (30 giây)

Tóm lại, phần học trực tuyến của em là **một nền tín hiệu SignalR, hai kiến trúc truyền hình ảnh**: lớp nhỏ thì nối thẳng máy-tới-máy cho nhẹ server; bài giảng một-đến-nhiều thì qua trạm chuyển tiếp tự viết. Em chọn kiến trúc theo bài toán chứ không theo trào lưu, và toàn bộ không phụ thuộc dịch vụ trả phí nào. Em xin hết phần trình bày, mong nhận được góp ý của thầy cô ạ.

---

## RÚT XUỐNG 5 PHÚT (khi bị giới hạn giờ)

Giữ nguyên: Mở đầu (1) — Ý tưởng kiến trúc 2D (2.1) — Demo 2D (2.3) — Vì sao SFU (3.1) — Demo 3D + replay (3.3) — Kết (5).
Cắt: bảy bước xây (2.2) nói gọn thành một câu *"em xây theo đúng bốn tầng Clean Architecture: dữ liệu, nghiệp vụ, giao diện, realtime"*; sáu bước 3D (3.2) bỏ hẳn; phần công nghệ (4) rút còn hai câu: không API key, không dịch vụ bên thứ ba, chỉ STUN miễn phí của Google, production thêm TURN.

---

## HỎI – ĐÁP DỰ PHÒNG

**"Sao không dùng Zoom / Google Meet?"** — Chủ động dữ liệu: điểm danh, chat, bản ghi gắn thẳng vào database của trung tâm; không phí license; và Zoom không nhúng được thí nghiệm 3D tương tác vào giữa buổi học.

**"STUN, TURN là gì?"** — STUN giúp máy tự biết địa chỉ công khai của mình, miễn phí, không thấy video. TURN là trạm trung chuyển khi hai máy không nối thẳng được — mạng 4G, NAT chặt; là chỗ duy nhất cần tài khoản khi triển khai thật.

**"Video có đi qua server không?"** — Phòng 2D: không, chạy thẳng máy-tới-máy. Phòng 3D: có, nhưng server chỉ chuyển tiếp gói tin thô, không giải mã, nên tải rất nhẹ.

**"Đông người thì sao?"** — Phòng 2D nghẽn ở phía client nên em giới hạn quy mô lớp nhỏ; đông hơn thì dùng kiến trúc trạm chuyển tiếp như phòng 3D — hệ thống đã có sẵn cả hai.

**"Học sinh vào trễ phòng 3D sao thấy đúng thí nghiệm?"** — Server lưu chuỗi hành động thay vì trạng thái; người vào sau phát lại chuỗi — như đọc lại tin nhắn cũ trong group chat.

**"Sao chat lưu database mà nét vẽ bảng trắng không lưu?"** — Chat cần lịch sử để học sinh xem lại; nét vẽ là dữ liệu tạm, khối lượng lớn, chỉ có giá trị trong buổi — muốn lưu thì đã có chức năng ghi hình cả buổi học.

**"Hệ thống có key nào không?"** — Có, ở phân hệ khác: cặp khóa VNPay cho thanh toán học phí, và JWT secret đang là placeholder phải thay trước khi deploy. Riêng phòng học online và phòng 3D không phụ thuộc key bên thứ ba nào.

**"Bảo mật phòng học dựa vào gì?"** — Bốn lớp: đăng nhập bắt buộc, danh tính đọc từ phiên ở phía server, phòng 3D thêm quyền lab + gói premium, và HTTPS + mã hóa DTLS-SRTP cho media.
