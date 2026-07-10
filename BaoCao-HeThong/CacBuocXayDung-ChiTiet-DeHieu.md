# CÁC BƯỚC XÂY DỰNG PHÒNG HỌC ONLINE & PHÒNG 3D — BẢN CHI TIẾT DỄ HIỂU

> Mỗi bước trình bày theo khung: **Mục tiêu** (làm gì) → **Cách làm** (làm thế nào,
> kể bằng lời) → **Vì sao** (lý do thiết kế — chính là chỗ ăn điểm khi bị hỏi).
> Không có dòng code nào. Đọc xong bản này là tự giảng lại được.

---

## PHẦN MỞ — 3 KHÁI NIỆM NỀN (nắm cái này trước, các bước sau tự sáng)

**1. SignalR là gì?** Web bình thường như hỏi–đáp: trình duyệt hỏi, server mới trả lời; server không tự nhiên "gọi" cho trình duyệt được. SignalR mở một **đường dây nóng hai chiều**: giữ kết nối liên tục, server chủ động đẩy tin xuống bất cứ lúc nào. Nhờ vậy một người gửi chat, cả phòng nhận ngay không cần bấm tải lại.

**2. WebRTC là gì?** Là **bộ đàm video gắn sẵn trong mọi trình duyệt** (Chrome, Edge, Safari đều có). Nó cho hai trình duyệt truyền hình ảnh, âm thanh **trực tiếp cho nhau** không cần qua server trung gian, và tự mã hóa toàn bộ. Google Meet, Messenger đều xây trên nó. Vì có sẵn trong trình duyệt nên miễn phí, không cần key.

**3. Vì sao cần cả hai?** Hai cái bộ đàm muốn dò được nhau thì lúc đầu phải có người **đưa thư hộ**: chuyển "lời mời", "lời đáp", "địa chỉ liên lạc" giữa hai bên. SignalR chính là người đưa thư đó. Đưa thư xong, hai máy nối thẳng, video tự chạy — người đưa thư đứng ngoài.

> Một câu tóm cả hệ thống: **SignalR chở tín hiệu, WebRTC chở hình ảnh.**

---

# GIAI ĐOẠN 1 — PHÒNG HỌC ONLINE 2D (7 bước)

## Bước 1 — Thiết kế dữ liệu

**Mục tiêu:** quyết định database phải nhớ những gì về một buổi học online.

**Cách làm:** ngồi liệt kê nghiệp vụ rồi quy về **5 bảng**:

1. **Buổi học** — tiêu đề, mã phòng, trạng thái, giờ hẹn / giờ bắt đầu thật / giờ kết thúc, sĩ số tối đa (mặc định 50), thuộc trường nào, giáo viên nào dạy.
2. **Người tham gia** — ai, vào lúc mấy giờ, rời lúc mấy giờ, đã được điểm danh chưa. Có giờ vào và giờ rời là tính được mỗi em ngồi học bao lâu.
3. **Tin nhắn chat** — ai gửi, nội dung, thời điểm.
4. **Slide** — file nào, bao nhiêu trang, ai tải lên.
5. **Bản ghi hình** — đường dẫn file video, dạy bởi ai, bao nhiêu người dự, dài bao lâu.

Trạng thái buổi học chạy như đèn giao thông một chiều: **Đã lên lịch → Đang diễn ra → Đã kết thúc** (nhánh phụ: Hủy). Mỗi lần chuyển trạng thái đều đóng dấu thời gian thật.

**Vì sao:** hệ thống nhiều trường dùng chung một database, nên **mọi bảng đều gắn mã trường** — truy vấn nào cũng lọc theo trường của người đăng nhập, trường này không bao giờ thấy dữ liệu trường kia. Riêng bảng bản ghi hình cố tình **chép cứng tên giáo viên, tên phòng** vào từng dòng (thay vì chỉ trỏ khóa ngoại) — vì phòng 3D không lưu buổi học trong database, không có gì để trỏ tới; chép cứng thì một bảng phục vụ được cả hai loại phòng.

## Bước 2 — Tạo bảng thật trong database (migration)

**Mục tiêu:** biến bản thiết kế trên giấy thành bảng thật.

**Cách làm:** khai báo cấu trúc từng bảng bằng file cấu hình riêng theo chuẩn dự án, rồi chạy lệnh migration — công cụ tự so sánh "database đang có" với "thiết kế mới" và sinh đúng phần chênh lệch.

**Vì sao:** migration như **sổ nhật ký thay đổi cấu trúc**: có thể tua lui khi hỏng, và đồng đội kéo code về chạy một lệnh là database giống hệt. Thực tế tính năng này đi qua **ba đợt migration** — bản gọn trước, bản đầy đủ sau, cuối cùng thêm ghi hình — cho thấy cách làm lặp, mở rộng dần thay vì cố làm hoàn hảo ngay từ đầu.

## Bước 3 — Viết lớp nghiệp vụ (bộ não của tính năng)

**Mục tiêu:** gom toàn bộ "luật chơi" vào một chỗ, trước khi đụng tới giao diện.

**Cách làm:** viết các hàm nghiệp vụ, mỗi hàm một luật:

1. **Tạo phòng** → tự sinh mã ngẫu nhiên dạng *bốn chữ – bốn số* (kiểu ABCD-1234). Chọn dạng này vì đọc được qua điện thoại, chép lên bảng không nhầm.
2. **Học sinh nhập mã** → kiểm tra đúng ba câu: mã có tồn tại trong trường mình không? phòng đã kết thúc chưa? phòng đã đầy chưa (đếm người *đang còn trong phòng*, và giáo viên không bị tính vào giới hạn)? Qua cả ba thì trả về **trọn gói một lần**: thông tin phòng + danh sách người + 50 tin chat gần nhất + slide + cờ "bạn có phải chủ phòng".
3. **Ghi nhận vào / rời phòng** → đóng dấu thời gian, phục vụ tính giờ học.
4. **Điểm danh** → giáo viên bấm tên em nào, ghi thẳng vào hồ sơ tham gia của em đó.
5. **Chat** → lưu database *trước*, phát cho cả phòng *sau*.
6. **Slide** → nhận file tải lên, lưu vào thư mục uploads của hệ thống.

Mọi hàm đều trả về "**thành công / thất bại + một câu tiếng Việt**" (ví dụ *"Phòng học đã đầy."*) — màn hình chỉ việc hiện nguyên câu, không tự chế thông báo.

**Vì sao trả trọn gói ở mục 2:** học sinh vào phòng chỉ tốn **một chuyến hỏi server** là có đủ mọi thứ để dựng màn hình — thay vì hỏi lắt nhắt bốn năm lần. **Vì sao tách lớp nghiệp vụ riêng:** sau này đổi giao diện (web sang mobile) không phải viết lại luật; và luật nằm riêng thì viết unit test được.

## Bước 4 — Làm các màn hình web

**Mục tiêu:** cho người dùng thao tác được bằng mắt và chuột.

**Cách làm:** năm màn hình — *danh sách buổi học*, *tạo phòng*, *nhập mã vào phòng*, *phòng học chính*, và một *lối vào riêng cho app mobile*. Luồng sử dụng trọn vẹn:

> Giáo viên tạo phòng → hệ thống hiện mã ABCD-1234 → giáo viên bấm **Phát phòng** (trạng thái chuyển Đang diễn ra) → gửi mã cho lớp qua Zalo/thông báo → học sinh vào trang *Tham gia*, gõ mã → cả hai cùng đứng trong phòng học chính.

**Vì sao:** màn hình phòng học chính chỉ là *cái khung* — video, chat, bảng trắng trong đó đều do hai bước kế tiếp thổi hồn vào. Tách vậy để giao diện và realtime không dính chùm nhau.

## Bước 5 — Dựng kênh realtime (tổng đài của phòng học)

**Mục tiêu:** mọi người trong phòng thấy cùng một thứ tại cùng một lúc.

**Cách làm:** trên đường dây nóng SignalR, mỗi buổi học là một **nhóm** — ai vào phòng thì được ghi tên vào nhóm đó, rời phòng thì xóa tên. Từ đây *mọi tính năng realtime chỉ là một loại tin nhắn phát cho cả nhóm*:

| Hành động | Tin nhắn phát đi | Có lưu database? |
|---|---|---|
| Gửi chat | "có tin nhắn mới" | **Có** — lưu trước, phát sau |
| Vẽ bảng trắng | "có nét vẽ mới" | Không — dữ liệu tạm |
| Giáo viên lật slide | "chuyển sang trang N" | Không (file slide thì có) |
| Giơ tay / hạ tay | "em A giơ tay" | Không |
| Điểm danh | "em A có mặt" | **Có** |
| Bắt đầu / kết thúc buổi | "phòng đã mở / đã đóng" | Có (đổi trạng thái) |

**Vì sao chat lưu mà nét vẽ không:** chat cần lịch sử — em nào tải lại trang vẫn đọc được; nét vẽ khối lượng lớn, chỉ có nghĩa trong buổi — muốn giữ thì đã có ghi hình cả buổi. **Về an toàn:** danh tính người gửi do **server tự đọc từ phiên đăng nhập** — trình duyệt không có chỗ nào để "tự xưng tên", nên không giả mạo được người khác.

## Bước 6 — Nối video: cú bắt tay WebRTC (bước quan trọng nhất)

**Mục tiêu:** mọi người trong phòng thấy mặt, nghe tiếng nhau — mà server không phải cõng video.

**Cách làm:** kể như chuyện A đang trong phòng, B mới bước vào:

1. **Xin quyền camera–micro.** Trình duyệt chỉ cho phép trên HTTPS (hoặc localhost) — đây là luật của trình duyệt, không phải của mình. Nếu người dùng từ chối camera, hệ thống lùi một bậc: chỉ dùng micro.
2. **B vào phòng** → tổng đài báo cho A: *"có B mới vào"*. A lập tức tạo một **ống nối** dành riêng cho B.
3. **A gửi lời mời** qua tổng đài — bản mô tả "tôi định phát tiếng và hình thế này".
4. **B đáp lời**, rồi hai bên trao nhau các **địa chỉ khả dĩ** để nối. Đây là lúc duy nhất cần "người ngoài": mỗi máy hỏi **máy chủ STUN công cộng miễn phí của Google** đúng một câu — *"địa chỉ công khai của tôi trên Internet là gì?"* — vì máy trong nhà chỉ biết địa chỉ nội bộ của nó. STUN không cần đăng ký, không cần key, và **không bao giờ nhìn thấy video**.
5. **Hai máy thử các đường** và chọn đường tốt nhất → từ giây đó, video chạy **thẳng máy-tới-máy**, tự mã hóa, tổng đài đứng ngoài hoàn toàn.

Chuyện tắt/bật cam, mic hay chia sẻ màn hình về sau chỉ là **thay làn tín hiệu trên ống nối sẵn có** — không phải bắt tay lại từ đầu, nên chuyển cảnh mượt.

**Vì sao gọi là mesh và vì sao giới hạn lớp nhỏ:** mỗi người nối với *từng* người khác — 8 người là mỗi máy gánh 7 luồng gửi đi. Server nhàn nhưng máy học sinh mệt dần theo sĩ số, nên kiến trúc này chủ đích cho lớp kèm 6–8 người. Đây chính là câu dẫn sang phòng 3D.

## Bước 7 — Ghi hình buổi học

**Mục tiêu:** học sinh vắng xem lại được; trung tâm có bằng chứng chất lượng buổi dạy.

**Cách làm:** dùng bộ ghi hình **có sẵn của trình duyệt** (MediaRecorder): bấm ghi là trình duyệt tự gom hình + tiếng thành file video định dạng webm. Kết thúc, file được tải lên server (chặn trần ~600 MB) kèm thông tin: buổi nào, ai dạy, bao nhiêu người, từ mấy giờ đến mấy giờ. Server cất file vào thư mục uploads, ghi một dòng vào bảng bản ghi, và có trang danh sách để mở xem lại.

**Vì sao ghi ở trình duyệt mà không ghi ở server:** phòng 2D video *không đi qua server* nên server không có gì để ghi — ghi tại trình duyệt là cách tự nhiên và không tốn thêm hạ tầng.

---

# GIAI ĐOẠN 2 — PHÒNG 3D LUMINA NEXUS (6 bước)

## Bước 1 — Nhận ra bài toán khác, đổi kiến trúc

**Mục tiêu:** chọn đúng cách truyền video cho kịch bản "một giáo viên → nhiều học sinh".

**Cách làm:** làm phép tính đơn giản. Lớp 20 em, nếu giữ kiểu máy-tới-máy: giáo viên phải **upload 20 luồng video cùng lúc** — mạng nhà nào chịu nổi. Đổi sang mô hình **đài truyền hình** (tên chuyên môn: SFU): giáo viên gửi **một luồng duy nhất** lên server; server làm trạm phát sóng, **sao chép gói tin** gửi cho từng em.

**Vì sao server chịu nổi:** vì trạm chỉ *chép và chuyển* gói tin, **không mở gói ra xử lý** (không giải mã, không nén lại) — công việc nhẹ như photo tài liệu, không phải đọc hiểu tài liệu.

## Bước 2 — Trang phòng 3D và cổng kiểm soát

**Mục tiêu:** có lối vào riêng, đúng người đúng gói dịch vụ.

**Cách làm:** một trang toàn màn hình riêng biệt, khóa **hai lớp**: người dùng phải có quyền vào phòng lab, *và* trung tâm phải đang ở **gói dịch vụ có tính năng phòng thí nghiệm ảo** (đây là tính năng premium để bán gói cao). Hai tiện ích nhỏ mà ăn điểm: hệ thống **tự đoán môn giáo viên đang dạy** (Hóa, Lý, Sinh, Toán) từ hồ sơ phân công để mở sẵn đúng bộ thí nghiệm; và **nút sao chép link mời** — bấm một cái là có đường dẫn kèm mã phòng gửi cho học sinh.

## Bước 3 — Tự viết trạm chuyển tiếp (SFU) bằng C#

**Mục tiêu:** có trạm phát sóng của riêng mình, không thuê ngoài.

**Cách làm:** dùng thư viện mã nguồn mở **SIPSorcery** (cài qua NuGet, không key, không phí) để server "nói được tiếng WebRTC". Cấu trúc trạm hình dung thế này:

1. Với **mỗi người phát** (giáo viên bật cam), server mở một **ống chỉ-nhận** hứng luồng của người đó.
2. Với **mỗi người xem**, server mở một **ống chỉ-gửi** riêng.
3. Gói tin nào rơi vào ống nhận → lập tức **chép sang tất cả ống gửi**. Hết.
4. Định dạng tiếng và hình được **chốt cứng một chuẩn** (Opus cho tiếng, VP8 cho hình — hai chuẩn mở mọi trình duyệt đều hiểu) để khỏi mất công "mặc cả định dạng" từng máy.

**Vì sao tự viết thay vì thuê:** dịch vụ ngoài (Agora, Twilio...) tính phí theo phút và yêu cầu API key, video học sinh chạy qua máy chủ của họ. Tự viết thì không đồng phí nào, dữ liệu ở lại hạ tầng của trung tâm — đổi lại đây là phần khó nhất của đồ án, và chính vì khó nên đáng trình bày.

## Bước 4 — Kênh tín hiệu riêng và "trí nhớ" của phòng

**Mục tiêu:** phòng 3D biết mình đang ở trạng thái nào để phục vụ người vào sau.

**Cách làm:** phòng 3D có tổng đài SignalR riêng, ngoài việc đưa thư còn **nhớ bốn thứ ngay trong bộ nhớ server**: ai đang ở phòng nào; phòng đang mở thí nghiệm gì; **chuỗi hóa chất đã đổ vào cốc** (theo đúng thứ tự); và các thông số mô phỏng đang chỉnh. Ai vào phòng là nhận ngay một gói "hiện trạng" đầy đủ để dựng lại đúng cảnh.

**Vì sao nhớ trong RAM chứ không ghi database:** phòng 3D là **phiên học tức thời** — buổi học tan là trạng thái hết giá trị, ghi database chỉ tốn công. Thứ cần giữ lâu dài là video thì đã có chức năng ghi hình lo.

## Bước 5 — Dựng thế giới 3D trên trình duyệt

**Mục tiêu:** phòng lab ảo nhìn thật, tương tác được, và mọi máy thấy giống nhau.

**Cách làm:** dùng **Three.js** — engine 3D mã nguồn mở chạy ngay trong trình duyệt, không cần cài phần mềm. Thư viện được **chép hẳn vào dự án tự host**, nên mất mạng ngoài vẫn chạy. Trong cảnh có: phòng lab với ánh sáng vật lý, mô hình mẫu vật **nhiều bộ phận** — tô sáng từng phần, kéo **tách rời như tranh lắp ghép** để xem cấu tạo, gắn nhãn chú thích tiếng Việt.

Đồng bộ chuyển động giải quyết khéo: giáo viên xoay mô hình thì máy giáo viên **chỉ gửi góc xoay 20 lần mỗi giây** (con số cân giữa mượt và nhẹ mạng); máy học sinh nhận các mốc rời rạc đó rồi **tự nội suy** — tự tính các khung hình trung gian — nên chuyển động vẫn mềm mại. Tiếng giáo viên còn được **gắn vào vị trí trong không gian**: học sinh di chuyển ra xa, tiếng nhỏ dần như ngoài đời.

## Bước 6 — Đồng bộ thí nghiệm: "ghi chuỗi hành động" (điểm nhấn nhất)

**Mục tiêu:** học sinh vào trễ vẫn thấy đúng hiện trường thí nghiệm.

**Cách làm:** theo dấu một thao tác — giáo viên đổ axit vào cốc:

1. Máy giáo viên chạy hiệu ứng (lọ bay lên, nghiêng, rót) và tra **bảng phản ứng hóa học** soạn theo sách giáo khoa Việt Nam để biết kết quả: đổi màu, sủi bọt, kết tủa, hay cháy (thả natri vào nước là có lửa).
2. Đồng thời báo tổng đài: *"vừa đổ axit"*. Tổng đài làm hai việc: **ghi hành động vào cuốn sổ của phòng**, rồi **phát cho cả lớp**.
3. Máy học sinh nhận tin, **chiếu lại đúng hiệu ứng đó** — mọi màn hình giống nhau.
4. Học sinh vào trễ: nhận **nguyên cuốn sổ** trong gói hiện trạng, phát lại cả chuỗi ở chế độ nhanh (không animation) — ra đúng màu dung dịch, đúng kết tủa, đúng phương trình cuối cùng.

**Vì sao hay:** server chỉ nhớ *chuỗi hành động* chứ không nhớ *kết quả* — nhẹ, không bao giờ lệch nhau, và người đến sau tự dựng lại được mọi thứ. Giới chuyên môn gọi đây là **event sourcing** thu nhỏ — nói được tên này kèm ví dụ group chat ("vào nhóm muộn, đọc lại tin nhắn cũ là hiểu chuyện") là rất thuyết phục.

---

# GIAI ĐOẠN 3 — CẤU HÌNH VÀ HOÀN THIỆN (3 việc)

**1. Khai địa chỉ STUN/TURN vào file cấu hình.** Hiện khai STUN miễn phí của Google — đủ cho demo và người dùng cùng mạng. File cấu hình **chừa sẵn chỗ cho TURN** — trạm trung chuyển cần khi học sinh dùng 4G hoặc mạng chặn nối thẳng; đây là **chỗ duy nhất trong toàn tính năng cần điền tài khoản–mật khẩu**, và có hai lựa chọn: tự dựng miễn phí bằng coturn trên VPS, hoặc thuê dịch vụ.

**2. Bảo đảm HTTPS toàn hệ thống.** Vừa là điều kiện trình duyệt cho mở camera, vừa bảo vệ kênh tín hiệu. Luồng video WebRTC thì **tự mã hóa theo chuẩn có sẵn** (DTLS-SRTP), không phải cài thêm gì.

**3. Chọn phòng theo quy mô** — thành quy tắc vận hành: lớp kèm ≤ 8 người bật cam → phòng 2D (server nhàn); bài giảng thí nghiệm một-đến-nhiều → phòng 3D qua trạm chuyển tiếp.

---

## TÓM TẮT MỘT TRANG (nhìn lướt trước giờ thuyết trình)

| # | Phòng 2D | Phòng 3D |
|---|---|---|
| 1 | Thiết kế 5 bảng dữ liệu + vòng đời buổi học | Tính toán → chọn kiến trúc trạm chuyển tiếp |
| 2 | Migration tạo bảng (3 đợt, làm dần) | Trang toàn màn hình + khóa 2 lớp premium |
| 3 | Lớp nghiệp vụ: sinh mã, 3 kiểm tra, trả trọn gói | Tự viết SFU bằng C# + SIPSorcery, chốt cứng codec |
| 4 | 5 màn hình, luồng tạo → phát → nhập mã | Tổng đài riêng + trí nhớ phòng trong RAM |
| 5 | Kênh realtime: mỗi buổi = 1 nhóm, mọi thứ là tin nhắn | Three.js: mô hình tách rời, xoay 20 lần/giây + nội suy |
| 6 | Bắt tay WebRTC 5 nhịp → video máy-tới-máy | Ghi chuỗi hành động → người vào trễ replay |
| 7 | Ghi hình tại trình duyệt → upload webm | — |

**Bên thứ ba, tóm gọn:** thư viện mã nguồn mở (SignalR, SIPSorcery, Three.js) + API sẵn trong trình duyệt (WebRTC, MediaRecorder) + một dịch vụ công cộng miễn phí (STUN Google). **Không API key, không phí dịch vụ**; production chỉ cần thêm TURN là chỗ duy nhất có tài khoản–mật khẩu.
