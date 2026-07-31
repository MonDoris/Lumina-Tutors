# -*- coding: utf-8 -*-
"""
Nội dung báo cáo đồ án môn học Thương mại điện tử — Đề tài: Hệ Thống Giáo Dục
Lumina Tutors (E-Learning).

Mỗi mục là một danh sách các khối nội dung theo DSL:
    ("p",   text)                       — đoạn văn thường
    ("b",   [item, ...])                — danh sách gạch đầu dòng
    ("fig", file, caption)              — hình đã sinh sẵn + chú thích
    ("ph",  caption, height_cm)         — khung chừa chỗ ảnh chụp màn hình + chú thích
    ("tbl", caption, headers, rows, widths_cm)  — bảng + chú thích phía trên
"""

TEN_DE_TAI = "Hệ Thống Giáo Dục Lumina Tutors (E-Learning)"

# ═══════════════════════════════════════════════════════════════════════════════
#  PHẦN ĐẦU
# ═══════════════════════════════════════════════════════════════════════════════

LOI_CAM_ON = [
    ("p", "Trước tiên, nhóm thực hiện xin gửi lời cảm ơn chân thành đến Ban Giám hiệu Trường Đại học "
          "Ngoại ngữ – Tin học Thành phố Hồ Chí Minh và Khoa Công nghệ Thông tin đã tạo điều kiện về "
          "chương trình đào tạo, cơ sở vật chất và môi trường học thuật để nhóm có cơ hội thực hiện đồ án "
          "môn học Thương mại điện tử."),
    ("p", "Nhóm xin bày tỏ lòng biết ơn sâu sắc tới giảng viên hướng dẫn — ThS. Nguyễn Thị Thúy A. "
          "Những định hướng của Cô về phạm vi đề tài, về cách gắn kết lý thuyết thương mại điện tử với "
          "một sản phẩm phần mềm cụ thể, cùng các góp ý chi tiết trong suốt quá trình thực hiện, là yếu tố "
          "quyết định giúp nhóm hoàn thiện cả phần xây dựng ứng dụng lẫn phần trình bày báo cáo."),
    ("p", "Nhóm cũng xin cảm ơn các thầy cô trong Khoa đã truyền đạt nền tảng kiến thức về phân tích thiết kế "
          "hệ thống, cơ sở dữ liệu, lập trình web và an toàn thông tin. Đây là nền tảng để nhóm triển khai "
          "hệ thống Lumina Tutors theo kiến trúc nhiều tầng, tích hợp cổng thanh toán và bảo đảm các yêu cầu "
          "bảo mật cho giao dịch trực tuyến."),
    ("p", "Do giới hạn về thời gian và kinh nghiệm thực tiễn, báo cáo chắc chắn còn những thiếu sót. Nhóm rất "
          "mong nhận được sự góp ý của Cô và các thầy cô trong hội đồng để đề tài được hoàn thiện hơn."),
    ("p", "Nhóm xin chân thành cảm ơn."),
]

# Bảng ký hiệu, chữ viết tắt và thuật ngữ (điền vào bảng có sẵn của mẫu)
VIET_TAT = [
    ("TMĐT", "Thương mại điện tử", "Hoạt động mua bán, cung ứng dịch vụ được tiến hành một phần hoặc toàn bộ trên môi trường mạng."),
    ("B2B", "Business-to-Business", "Mô hình giao dịch giữa doanh nghiệp với doanh nghiệp. Trong đề tài: nền tảng bán gói dịch vụ cho nhà trường."),
    ("B2C", "Business-to-Consumer", "Mô hình giao dịch giữa doanh nghiệp với người tiêu dùng. Trong đề tài: nhà trường thu học phí từ phụ huynh."),
    ("SaaS", "Software as a Service", "Mô hình cung cấp phần mềm dưới dạng dịch vụ thuê bao theo chu kỳ."),
    ("MVC", "Model – View – Controller", "Mẫu kiến trúc phân tách dữ liệu, giao diện và điều khiển của ứng dụng web."),
    ("EF Core", "Entity Framework Core", "Thư viện ánh xạ đối tượng – quan hệ (ORM) của nền tảng .NET."),
    ("ORM", "Object–Relational Mapping", "Kỹ thuật ánh xạ giữa đối tượng trong mã nguồn và bảng trong cơ sở dữ liệu."),
    ("API", "Application Programming Interface", "Giao diện lập trình cho phép các hệ thống trao đổi dữ liệu với nhau."),
    ("REST", "Representational State Transfer", "Kiểu kiến trúc thiết kế API dựa trên giao thức HTTP."),
    ("JWT", "JSON Web Token", "Chuẩn token dùng để xác thực và truyền tải thông tin định danh giữa các bên."),
    ("RBAC", "Role-Based Access Control", "Cơ chế phân quyền truy cập dựa trên vai trò người dùng."),
    ("IPN", "Instant Payment Notification", "Thông báo thanh toán tức thời do cổng thanh toán gửi tới máy chủ bán hàng."),
    ("HMAC", "Hash-based Message Authentication Code", "Mã xác thực thông điệp dựa trên hàm băm, dùng để ký và kiểm tra tính toàn vẹn dữ liệu."),
    ("OLTP", "Online Transaction Processing", "Xử lý giao dịch trực tuyến — nhóm nghiệp vụ ghi nhận và bảo đảm tính nhất quán của giao dịch."),
    ("SEO", "Search Engine Optimization", "Tối ưu hóa nội dung nhằm nâng thứ hạng hiển thị trên công cụ tìm kiếm."),
    ("XSS", "Cross-Site Scripting", "Lỗ hổng cho phép chèn mã kịch bản độc hại vào trang web."),
    ("CSRF", "Cross-Site Request Forgery", "Lỗ hổng giả mạo yêu cầu từ phía người dùng đã đăng nhập."),
    ("SQLi", "SQL Injection", "Lỗ hổng chèn câu lệnh SQL độc hại qua dữ liệu đầu vào."),
    ("TLS", "Transport Layer Security", "Giao thức mã hóa dữ liệu trên đường truyền, nền tảng của HTTPS."),
    ("HSTS", "HTTP Strict Transport Security", "Cơ chế buộc trình duyệt chỉ kết nối tới máy chủ qua HTTPS."),
    ("DTO", "Data Transfer Object", "Đối tượng trung gian dùng để truyền dữ liệu giữa các tầng của ứng dụng."),
    ("ERD", "Entity Relationship Diagram", "Sơ đồ thực thể – liên kết mô tả cấu trúc cơ sở dữ liệu."),
    ("CI/CD", "Continuous Integration / Continuous Deployment", "Tích hợp liên tục và triển khai liên tục."),
    ("QR", "Quick Response Code", "Mã vạch hai chiều, trong hệ thống được dùng cho điểm danh và tra cứu hóa đơn."),
    ("Entitlement", "Quyền sử dụng tính năng", "Trạng thái cho phép một trường truy cập tính năng cao cấp sau khi đã thanh toán."),
    ("Quota", "Hạn mức", "Giới hạn số lượng tài khoản hoặc lớp học mà một gói dịch vụ cho phép tạo."),
]

# ═══════════════════════════════════════════════════════════════════════════════
#  CHƯƠNG 1 — TỔNG QUAN
# ═══════════════════════════════════════════════════════════════════════════════

C1_1 = [
    ("p", "Thương mại điện tử đã chuyển từ vị trí một kênh bán hàng bổ trợ thành hạ tầng vận hành cốt lõi của "
          "phần lớn doanh nghiệp. Sự dịch chuyển này bắt nguồn từ ba điều kiện chín muồi cùng lúc: hạ tầng "
          "Internet băng rộng và thiết bị di động phổ cập, hệ thống thanh toán trực tuyến được chuẩn hóa, và "
          "hành lang pháp lý thừa nhận giá trị của dữ liệu điện tử trong giao dịch dân sự."),
    ("p", "Tại Việt Nam, khung pháp lý cho thương mại điện tử được xác lập bởi Nghị định số 52/2013/NĐ-CP về "
          "thương mại điện tử, được sửa đổi và bổ sung bởi Nghị định số 85/2021/NĐ-CP. Bên cạnh đó, Luật Giao "
          "dịch điện tử số 20/2023/QH15 có hiệu lực từ ngày 01 tháng 7 năm 2024 đã thay thế Luật Giao dịch điện "
          "tử năm 2005, mở rộng phạm vi điều chỉnh và làm rõ giá trị pháp lý của thông điệp dữ liệu, chữ ký "
          "điện tử cũng như việc chuyển đổi giữa bản giấy và bản điện tử. Đây là cơ sở pháp lý trực tiếp cho "
          "các nghiệp vụ đặt hàng, thanh toán và xuất chứng từ trực tuyến mà đề tài triển khai."),
    ("p", "Về quy mô thị trường, thương mại điện tử tiếp tục là một trong những lĩnh vực tăng trưởng nhanh nhất "
          "của nền kinh tế số Việt Nam. Theo các báo cáo ngành công bố trong năm 2025, quy mô thị trường bán lẻ "
          "trực tuyến đã vượt mốc 30 tỷ USD với tốc độ tăng trưởng duy trì ở mức hai chữ số, đưa Việt Nam vào "
          "nhóm các quốc gia có tốc độ tăng trưởng thương mại điện tử nhanh nhất khu vực. Tốc độ này cho thấy "
          "kỹ năng thiết kế và vận hành một hệ thống thương mại điện tử đã trở thành năng lực nghề nghiệp bắt "
          "buộc đối với sinh viên ngành Công nghệ Thông tin."),
    ("p", "Điểm đáng chú ý là thương mại điện tử không còn bó hẹp trong phạm vi hàng hóa hữu hình. Nhóm sản phẩm "
          "số — phần mềm thuê bao, khóa học trực tuyến, nội dung số — chiếm tỷ trọng ngày càng lớn và có đặc thù "
          "riêng: chi phí biên gần bằng không, khâu giao hàng được thay bằng việc cấp quyền truy cập tức thời, "
          "và doanh thu mang tính định kỳ thay vì đơn lẻ. Chính đặc thù này đặt ra những bài toán kỹ thuật khác "
          "với thương mại điện tử truyền thống: quản lý vòng đời thuê bao, kiểm soát hạn mức sử dụng, xử lý gia "
          "hạn tự động và bảo đảm tính idempotent của giao dịch thanh toán."),
    ("p", "Trong lĩnh vực giáo dục, quá trình chuyển đổi số đang diễn ra ở cả hai chiều. Ở chiều thứ nhất, các "
          "trường học và trung tâm đào tạo trở thành khách hàng của những nền tảng phần mềm quản lý được cung cấp "
          "theo mô hình thuê bao. Ở chiều thứ hai, chính các trường lại dùng nền tảng đó để số hóa quan hệ tài "
          "chính với phụ huynh, thay thế việc thu học phí bằng tiền mặt bằng hóa đơn điện tử và thanh toán qua "
          "cổng trung gian. Đề tài này được xây dựng để giải quyết đồng thời cả hai chiều đó."),
]

C1_2 = [
    ("p", "Lumina Tutors là nền tảng quản lý giáo dục đa trường (multi-tenant) hướng tới các trường tư thục và "
          "trung tâm đào tạo tại Việt Nam. Sản phẩm số hóa toàn bộ quy trình vận hành của một cơ sở giáo dục: "
          "quản lý học vụ, điểm danh, sổ điểm theo Thông tư 22/2021/TT-BGDĐT, tài chính – học phí, nhân sự, kỷ "
          "luật, giao tiếp với phụ huynh, cùng các tính năng học tập số như khóa học trực tuyến, lớp học thời "
          "gian thực, phòng thí nghiệm ảo và trợ giảng AI."),
    ("p", "Xét trên góc độ thương mại điện tử, Lumina Tutors vận hành hai tuyến giao dịch tách bạch nhưng liên "
          "thông với nhau:"),
    ("b", [
        "Tuyến B2B: nền tảng đóng vai trò nhà cung cấp phần mềm, bán gói dịch vụ theo chu kỳ tháng – quý – năm "
        "cho các trường. Khách hàng là nhà trường, người ra quyết định mua là quản trị viên trường, sản phẩm là "
        "quyền sử dụng phần mềm kèm hạn mức tài khoản và các tính năng cao cấp mua thêm.",
        "Tuyến B2C: trong phạm vi mỗi trường, nhà trường phát hành hóa đơn học phí tới học sinh và phụ huynh; "
        "phụ huynh thanh toán trực tuyến qua cổng VNPay hoặc thanh toán ngoại tuyến và được kế toán xác nhận. "
        "Nền tảng đóng vai trò hạ tầng giao dịch cho tuyến này.",
    ]),
    ("p", "Cơ cấu doanh thu vì vậy gồm hai nguồn: doanh thu thuê bao định kỳ từ gói dịch vụ và tính năng bán lẻ "
          "(mô hình SaaS), và giá trị gia tăng từ việc chuẩn hóa dòng tiền học phí cho nhà trường. Mô hình này "
          "được phản ánh trực tiếp trong thiết kế cơ sở dữ liệu: nhóm bảng quản lý thuê bao phục vụ tuyến B2B, "
          "nhóm bảng tài chính – học phí phục vụ tuyến B2C, và cả hai cùng dùng chung một hạ tầng thanh toán."),
    ("p", "Hệ thống phục vụ sáu vai trò nghiệp vụ tương ứng với sáu nhóm công việc khác nhau trong một cơ sở giáo "
          "dục, cùng một vai trò quản trị cấp nền tảng. Bảng 1.1 mô tả các vai trò này và mối liên hệ với hoạt "
          "động thương mại điện tử của hệ thống."),
    ("tbl", "Các vai trò người dùng của hệ thống Lumina Tutors",
     ["Vai trò", "Phạm vi công việc", "Liên quan tới nghiệp vụ TMĐT"],
     [
        ["Quản trị nền tảng (SYSADMIN)", "Quản lý danh mục gói dịch vụ, khởi tạo tài khoản trường, theo dõi doanh thu, hỗ trợ khách hàng.", "Người bán trong tuyến B2B"],
        ["Quản trị trường (ADMIN)", "Cấu hình năm học, lớp, nhân sự; quyết định mua và gia hạn gói dịch vụ.", "Người mua trong tuyến B2B"],
        ["Kế toán (ACCOUNTANT)", "Cấu hình biểu phí, phát hành hóa đơn học phí, theo dõi công nợ, xác nhận thanh toán.", "Người bán trong tuyến B2C"],
        ["Giáo viên (TEACHER)", "Điểm danh, nhập điểm, biên soạn và gán khóa học trực tuyến.", "Tạo nội dung số được bàn giao"],
        ["Giám thị (SUPERVISOR)", "Ghi nhận kỷ luật, gửi thông báo tới phụ huynh.", "Kênh truyền thông tới người dùng cuối"],
        ["Học sinh (STUDENT)", "Xem điểm, điểm danh, học khóa học trực tuyến, dùng trợ giảng AI.", "Người thụ hưởng dịch vụ số"],
        ["Phụ huynh (PARENT)", "Theo dõi kết quả học tập, xem và thanh toán hóa đơn học phí.", "Người mua trong tuyến B2C"],
     ],
     [3.6, 6.4, 5.5]),
    ("p", "Về công nghệ, hệ thống được xây dựng trên nền tảng ASP.NET Core 8.0 theo mẫu MVC, sử dụng Entity "
          "Framework Core 8 làm lớp truy cập dữ liệu và SQL Server làm hệ quản trị cơ sở dữ liệu. Phần ứng dụng "
          "di động dành cho giáo viên, học sinh, phụ huynh và giám thị được phát triển bằng React Native trên nền "
          "Expo, giao tiếp với máy chủ qua REST API bảo vệ bằng JWT. Các tính năng thời gian thực như lớp học "
          "trực tuyến và phòng thí nghiệm ảo dùng SignalR trên nền WebSocket."),
]

# ═══════════════════════════════════════════════════════════════════════════════
#  CHƯƠNG 2 — CƠ SỞ LÝ THUYẾT
# ═══════════════════════════════════════════════════════════════════════════════

C2_1 = [
    ("p", "Thương mại điện tử được hiểu theo nhiều phạm vi rộng hẹp khác nhau tùy góc nhìn. Theo cách tiếp cận "
          "của Tổ chức Hợp tác và Phát triển Kinh tế (OECD), giao dịch thương mại điện tử là việc mua hoặc bán "
          "hàng hóa, dịch vụ được thực hiện qua mạng máy tính bằng những phương thức được thiết kế riêng cho mục "
          "đích nhận hoặc đặt đơn hàng; việc thanh toán và giao hàng không nhất thiết phải diễn ra trực tuyến."),
    ("p", "Ở góc nhìn học thuật, Laudon và Traver định nghĩa thương mại điện tử là việc sử dụng Internet, World "
          "Wide Web và các ứng dụng di động để thực hiện những giao dịch thương mại có tính chất trao đổi giá trị "
          "giữa các tổ chức hoặc cá nhân. Định nghĩa này nhấn mạnh yếu tố “trao đổi giá trị”, tức là phải có sự "
          "chuyển giao quyền sở hữu hoặc quyền sử dụng để đổi lấy một khoản thanh toán."),
    ("p", "Trong hệ thống pháp luật Việt Nam, Nghị định số 52/2013/NĐ-CP (được sửa đổi, bổ sung bởi Nghị định số "
          "85/2021/NĐ-CP) xác định hoạt động thương mại điện tử là việc tiến hành một phần hoặc toàn bộ quy trình "
          "của hoạt động thương mại bằng phương tiện điện tử có kết nối mạng Internet, mạng viễn thông di động "
          "hoặc các mạng mở khác. Cách định nghĩa “một phần hoặc toàn bộ” là điểm quan trọng đối với đề tài: một "
          "hệ thống vẫn được coi là thương mại điện tử ngay cả khi cho phép song song hình thức thanh toán ngoại "
          "tuyến, miễn là các khâu chào bán, đặt hàng và ghi nhận giao dịch được thực hiện trên môi trường mạng."),
    ("p", "Cần phân biệt thương mại điện tử (e-commerce) với kinh doanh điện tử (e-business). Thương mại điện tử "
          "tập trung vào các giao dịch có phát sinh trao đổi giá trị với bên ngoài tổ chức, trong khi kinh doanh "
          "điện tử bao trùm cả những quy trình nội bộ được số hóa nhưng không trực tiếp tạo ra doanh thu. Theo "
          "cách phân biệt này, phân hệ quản lý thuê bao và phân hệ học phí của Lumina Tutors thuộc phạm vi thương "
          "mại điện tử, còn các phân hệ điểm danh, sổ điểm hay kỷ luật thuộc phạm vi kinh doanh điện tử."),
    ("p", "Thương mại điện tử có một số đặc trưng phân biệt rõ với thương mại truyền thống: tính phổ quát (giao "
          "dịch không bị giới hạn bởi vị trí địa lý và thời gian mở cửa), tính phong phú của thông tin sản phẩm, "
          "khả năng tương tác hai chiều, khả năng cá nhân hóa theo từng khách hàng, và mật độ thông tin cao cho "
          "phép so sánh giá và chất lượng gần như tức thời. Với sản phẩm số, các đặc trưng này còn được cộng "
          "hưởng bởi chi phí sao chép gần bằng không và khả năng bàn giao tức thời."),
]

C2_2 = [
    ("p", "Các mô hình kinh doanh trong thương mại điện tử thường được phân loại theo hai tiêu chí: theo bản chất "
          "các bên tham gia giao dịch và theo cơ chế tạo doanh thu."),
    ("p", "Phân loại theo các bên tham gia gồm những mô hình phổ biến sau: B2B (doanh nghiệp với doanh nghiệp), "
          "B2C (doanh nghiệp với người tiêu dùng), C2C (người tiêu dùng với người tiêu dùng), C2B (người tiêu dùng "
          "với doanh nghiệp) và B2G (doanh nghiệp với cơ quan nhà nước). Bảng 2.1 so sánh những đặc điểm chính của "
          "các mô hình này và chỉ ra vị trí của đề tài."),
    ("tbl", "So sánh các mô hình kinh doanh thương mại điện tử",
     ["Mô hình", "Đặc điểm giao dịch", "Ví dụ điển hình", "Áp dụng trong đề tài"],
     [
        ["B2B", "Giá trị đơn hàng lớn, chu kỳ quyết định dài, thường có thương lượng và hợp đồng dài hạn.", "Alibaba, Salesforce, Microsoft 365", "Nền tảng bán gói dịch vụ cho nhà trường"],
        ["B2C", "Giá trị đơn hàng nhỏ, số lượng giao dịch lớn, quyết định mua nhanh.", "Shopee, Tiki, Netflix", "Nhà trường thu học phí từ phụ huynh"],
        ["C2C", "Nền tảng trung gian kết nối các cá nhân, doanh thu từ phí giao dịch.", "Chợ Tốt, eBay", "Không áp dụng"],
        ["C2B", "Cá nhân cung cấp sản phẩm hoặc dịch vụ cho doanh nghiệp.", "Nền tảng freelance, kho ảnh", "Không áp dụng"],
        ["B2G", "Doanh nghiệp cung cấp hàng hóa, dịch vụ cho cơ quan nhà nước.", "Đấu thầu qua mạng", "Không áp dụng"],
     ],
     [1.9, 5.2, 4.0, 4.4]),
    ("p", "Phân loại theo cơ chế tạo doanh thu gồm các mô hình: bán hàng trực tiếp, thuê bao định kỳ "
          "(subscription), phí giao dịch, quảng cáo, hoa hồng liên kết và mô hình freemium. Lumina Tutors áp dụng "
          "mô hình thuê bao định kỳ có phân tầng, một biến thể phổ biến của SaaS, với ba đặc điểm sau:"),
    ("b", [
        "Phân tầng theo gói: khách hàng chọn một trong các gói có mức giá và hạn mức khác nhau; gói cao hơn "
        "bao gồm sẵn các tính năng cao cấp.",
        "Bán thêm theo tính năng (add-on): trường dùng gói thấp vẫn có thể mua lẻ từng tính năng cao cấp thay vì "
        "buộc phải nâng cấp toàn bộ gói.",
        "Bán thêm theo hạn mức (quota add-on): khi số lượng tài khoản hoặc lớp học vượt hạn mức của gói, trường "
        "mua thêm hạn mức bổ sung mà không thay đổi gói đang dùng.",
    ]),
    ("p", "Ưu điểm của mô hình thuê bao là doanh thu định kỳ có thể dự báo được và chi phí phục vụ khách hàng "
          "hiện hữu thấp hơn nhiều so với chi phí thu hút khách hàng mới. Nhược điểm là hệ thống phải xử lý chính "
          "xác vòng đời thuê bao — đăng ký, nâng cấp, mua thêm, gia hạn, hết hạn và hủy — vì bất kỳ sai lệch nào "
          "cũng dẫn tới việc cấp sai quyền sử dụng hoặc thất thoát doanh thu."),
]

C2_3 = [
    ("p", "Một hệ thống thương mại điện tử hoàn chỉnh thường được mô tả bằng sáu nhóm thành phần chức năng. Mục "
          "này trình bày vai trò lý thuyết của từng nhóm, làm cơ sở đối chiếu với phần xây dựng ứng dụng ở "
          "Chương 3."),
    ("p", "Thứ nhất là nhóm giao diện bán hàng (front-office). Nhóm này gồm danh mục sản phẩm, công cụ tìm kiếm "
          "và lọc, trang chi tiết sản phẩm, giỏ hàng và quy trình thanh toán. Yêu cầu cốt lõi là rút ngắn quãng "
          "đường từ lúc khách hàng quan tâm tới lúc hoàn tất đơn hàng."),
    ("p", "Thứ hai là nhóm quản trị bán hàng (back-office), gồm quản lý danh mục và giá, quản lý đơn hàng, quản "
          "lý tồn kho hoặc hạn mức, và báo cáo doanh thu. Với sản phẩm số, khái niệm tồn kho được thay bằng hạn "
          "mức sử dụng và trạng thái quyền truy cập."),
    ("p", "Thứ ba là nhóm thanh toán. Hệ thống bán hàng hầu như không tự xử lý thông tin thẻ mà ủy quyền cho cổng "
          "thanh toán trung gian; trách nhiệm của hệ thống là tạo yêu cầu thanh toán có chữ ký, tiếp nhận kết quả "
          "trả về, xác minh chữ ký và cập nhật trạng thái đơn hàng một cách nhất quán."),
    ("p", "Thứ tư là nhóm giao nhận. Với hàng hóa hữu hình, nhóm này tích hợp với các đơn vị vận chuyển để tính "
          "phí, tạo vận đơn và theo dõi hành trình. Với sản phẩm số, giao nhận được thay bằng việc kích hoạt "
          "quyền truy cập và gửi chứng từ điện tử cho khách hàng ngay sau khi thanh toán được xác nhận."),
    ("p", "Thứ năm là nhóm tiếp thị điện tử, gồm quản lý nội dung, tối ưu hóa công cụ tìm kiếm, cá nhân hóa nội "
          "dung theo hành vi người dùng và các kênh truyền thông trực tiếp như email, thông báo đẩy và trò chuyện "
          "trực tuyến."),
    ("p", "Thứ sáu là nhóm nền tảng kỹ thuật xuyên suốt: xác thực và phân quyền, mã hóa đường truyền, chống các "
          "tấn công phổ biến vào ứng dụng web, ghi nhật ký giao dịch phục vụ đối soát, cùng hạ tầng triển khai và "
          "giám sát. Bảng 2.2 đối chiếu sáu nhóm thành phần này với hiện trạng cài đặt trong Lumina Tutors."),
    ("tbl", "Đối chiếu thành phần hệ thống TMĐT với hiện trạng cài đặt trong Lumina Tutors",
     ["Nhóm thành phần", "Chức năng lý thuyết", "Hiện trạng trong Lumina Tutors"],
     [
        ["Giao diện bán hàng", "Danh mục, tìm kiếm, giỏ hàng, thanh toán", "Trang danh mục gói và add-on; trang đơn hàng cho phép đổi chu kỳ và xem tổng tiền trước khi thanh toán"],
        ["Quản trị bán hàng", "Quản lý giá, đơn hàng, tồn kho, báo cáo", "Màn hình quản trị danh mục gói/add-on; danh sách đơn đã thanh toán; báo cáo doanh thu; hạn mức thay cho tồn kho"],
        ["Thanh toán", "Tạo yêu cầu, nhận kết quả, xác minh, đối soát", "Tích hợp cổng VNPay cho cả học phí và gói dịch vụ, có IPN và trang kết quả"],
        ["Giao nhận", "Vận chuyển hoặc bàn giao sản phẩm", "Bàn giao số: kích hoạt quyền dùng tính năng và gửi biên nhận qua email"],
        ["Tiếp thị điện tử", "Nội dung, SEO, cá nhân hóa, truyền thông", "Trang giới thiệu, bảng tin trường, thông báo trong ứng dụng, email hóa đơn, chat hỗ trợ, trợ giảng AI"],
        ["Nền tảng kỹ thuật", "Bảo mật, nhật ký, triển khai", "Xác thực Cookie và JWT, chín chính sách phân quyền, chống CSRF/XSS/SQLi, nhật ký Serilog"],
     ],
     [3.3, 4.5, 7.7]),
]

# ═══════════════════════════════════════════════════════════════════════════════
#  CHƯƠNG 3 — XÂY DỰNG ỨNG DỤNG
# ═══════════════════════════════════════════════════════════════════════════════

C3_1 = [
    ("p", "Hệ thống được tổ chức theo kiến trúc Clean Architecture gồm bốn tầng, trong đó mỗi tầng chỉ được phép "
          "phụ thuộc vào tầng nằm sâu hơn. Nguyên tắc này bảo đảm phần lõi nghiệp vụ — nơi đặt các quy tắc về giá, "
          "hạn mức và vòng đời thuê bao — không bị ràng buộc vào công nghệ giao diện hay hệ quản trị cơ sở dữ "
          "liệu cụ thể, nhờ đó có thể kiểm thử độc lập và thay thế hạ tầng khi cần. Hình 3.1 mô tả cấu trúc bốn "
          "tầng cùng khối lượng thành phần thực tế của từng tầng."),
    ("fig", "h3_1_kien_truc.png", "Kiến trúc bốn tầng của hệ thống Lumina Tutors", 13.0),
    ("p", "Tầng Domain chứa 85 lớp thực thể được tổ chức theo từng phân hệ nghiệp vụ, các kiểu liệt kê và hai "
          "giao diện trừu tượng cho mẫu Repository và Unit of Work. Tầng này không tham chiếu bất kỳ thư viện "
          "ngoài nào. Ba lớp cơ sở được dùng cho toàn bộ thực thể: lớp chỉ mang khóa chính, lớp bổ sung mốc thời "
          "gian tạo và cập nhật, và lớp bổ sung mã trường phục vụ cơ chế đa đơn vị (multi-tenant)."),
    ("p", "Tầng Application chứa 25 dịch vụ nghiệp vụ, mỗi dịch vụ phụ trách một phân hệ. Toàn bộ phương thức "
          "công khai đều trả về kiểu kết quả bao gói (Result), buộc nơi gọi phải kiểm tra trạng thái thành công "
          "trước khi sử dụng dữ liệu và trả về thông điệp lỗi bằng tiếng Việt để hiển thị trực tiếp cho người "
          "dùng. Cách tiếp cận này loại bỏ việc dùng ngoại lệ làm cơ chế điều khiển luồng cho các lỗi nghiệp vụ "
          "thông thường như hết hạn mức hay đơn hàng đã được thanh toán."),
    ("p", "Tầng Infrastructure cài đặt các giao diện của tầng Domain bằng Entity Framework Core, kèm bộ chặn tự "
          "động gán mốc thời gian cho thực thể, các lớp cấu hình ánh xạ và bộ dữ liệu mẫu cho môi trường phát "
          "triển. Tầng Web gồm 32 controller, 136 khung nhìn Razor, ba hub thời gian thực và nhóm API dành cho "
          "ứng dụng di động."),
    ("p", "Về phạm vi thương mại điện tử, hệ thống được phân rã thành hai nhóm chức năng chính là Electronic "
          "Selling và Electronic Marketing, tương ứng với hai nhóm ca sử dụng trong Hình 3.2. Ba tác nhân tham "
          "gia gồm quản trị nền tảng ở vai trò người bán tuyến B2B, nhà trường vừa là người mua tuyến B2B vừa là "
          "người bán tuyến B2C, và phụ huynh – học sinh ở vai trò người dùng cuối."),
    ("fig", "h3_2_usecase.png", "Sơ đồ ca sử dụng tổng quan hai nhóm chức năng Electronic Selling và Electronic Marketing", 14.5),
    ("p", "Điểm cần lưu ý trong Hình 3.2 là ca sử dụng “Thanh toán trực tuyến” được bốn ca sử dụng khác bao hàm "
          "theo quan hệ «include». Đây là kết quả của một quyết định thiết kế quan trọng: toàn bộ nghiệp vụ phát "
          "sinh tiền — đăng ký gói, nâng cấp, mua add-on, gia hạn và thu học phí — đều quy về một luồng thanh "
          "toán thống nhất, giúp mã nguồn xử lý cổng thanh toán chỉ tồn tại ở một nơi và giảm rủi ro sai lệch "
          "giữa các luồng."),
    ("p", "Cấu trúc dữ liệu của phân hệ thương mại điện tử được trình bày trong Hình 3.3. Sơ đồ này chỉ thể hiện "
          "mười bảng liên quan trực tiếp tới hai tuyến giao dịch, trích từ tổng số 85 bảng của toàn hệ thống."),
    ("fig", "h3_3_erd.png", "Sơ đồ thực thể – liên kết của phân hệ thương mại điện tử", 15.5),
    ("p", "Nhóm bảng phía trên phục vụ tuyến B2B. Bảng danh mục gói và danh mục add-on đóng vai trò catalog sản "
          "phẩm ở cấp nền tảng, không thuộc về trường nào. Bảng đăng ký của trường lưu trạng thái thuê bao hiện "
          "hành, gồm gói đang dùng, chu kỳ thanh toán, ngày kết thúc kỳ và cờ tự động gia hạn. Mỗi lần phát sinh "
          "giao dịch, hệ thống tạo một bản ghi đơn hàng kèm các dòng chi tiết, tương tự cặp bảng đơn hàng – dòng "
          "đơn hàng trong hệ thống bán lẻ truyền thống."),
    ("p", "Nhóm bảng phía dưới phục vụ tuyến B2C. Bảng cấu hình biểu phí định nghĩa các khoản thu theo năm học và "
          "khối lớp; từ đó hệ thống sinh hóa đơn cho từng học sinh theo kỳ thanh toán. Mỗi hóa đơn có thể ghi "
          "nhận nhiều lần thanh toán, cho phép xử lý trường hợp phụ huynh nộp thành nhiều đợt — trạng thái hóa "
          "đơn khi đó chuyển sang “thanh toán một phần”."),
    ("p", "Từ sơ đồ ca sử dụng và cấu trúc dữ liệu nêu trên, Bảng 3.1 phân rã các nhóm chức năng thương mại điện "
          "tử theo tác nhân thực hiện và kết quả nghiệp vụ tương ứng. Bảng này là cơ sở để triển khai chi tiết ở "
          "các mục tiếp theo của chương."),
    ("tbl", "Phân rã chức năng thương mại điện tử theo tác nhân",
     ["Nhóm chức năng", "Tác nhân thực hiện", "Kết quả nghiệp vụ"],
     [
        ["Quản trị danh mục gói và add-on", "Quản trị nền tảng", "Cập nhật giá, hạn mức, trạng thái kinh doanh của sản phẩm"],
        ["Xem catalog và đăng ký gói", "Quản trị trường", "Sinh đơn hàng ở trạng thái chờ thanh toán"],
        ["Mua add-on và hạn mức bổ sung", "Quản trị trường", "Mở rộng tính năng hoặc hạn mức trong kỳ hiện hành"],
        ["Gia hạn và tự động gia hạn", "Quản trị trường / tác vụ định kỳ", "Kéo dài kỳ hiệu lực của thuê bao"],
        ["Cấu hình biểu phí và phát hành hóa đơn", "Kế toán trường", "Sinh hóa đơn học phí hàng loạt theo kỳ"],
        ["Thanh toán trực tuyến", "Quản trị trường / phụ huynh", "Ghi nhận giao dịch, cập nhật trạng thái, kích hoạt quyền dùng"],
        ["Theo dõi doanh thu và công nợ", "Quản trị nền tảng / kế toán", "Báo cáo doanh thu thuê bao và dư nợ học phí"],
     ],
     [5.0, 4.6, 5.9]),
]

C3_2_0 = [
    ("p", "Electronic Selling là nhóm chức năng hỗ trợ toàn bộ quá trình bán hàng trên môi trường số, từ khâu "
          "trưng bày sản phẩm tới khâu ghi nhận giao dịch hoàn tất. Trong Lumina Tutors, nhóm chức năng này được "
          "cài đặt cho cả hai loại sản phẩm: gói dịch vụ phần mềm bán cho nhà trường và hóa đơn học phí phát hành "
          "cho phụ huynh."),
]

C3_2_1 = [
    ("p", "Danh mục sản phẩm của tuyến B2B gồm ba loại mặt hàng có bản chất khác nhau nhưng dùng chung cơ chế "
          "định giá theo ba chu kỳ tháng, quý và năm."),
    ("p", "Loại thứ nhất là gói dịch vụ. Mỗi gói được xác định bằng mã gói, tên hiển thị, mô tả, bậc gói dùng để "
          "so sánh khi nâng cấp, ba mức giá theo chu kỳ, các cờ cho biết gói đã bao gồm sẵn tính năng cao cấp nào, "
          "và bảy hạn mức tài khoản theo từng vai trò cùng hạn mức số lớp học. Giá trị âm một được quy ước là "
          "không giới hạn. Bảng 3.2 trình bày hai gói đang được cấu hình trong hệ thống."),
    ("tbl", "Danh mục gói dịch vụ",
     ["Thuộc tính", "Gói Cơ Bản (BASIC)", "Gói Cao Cấp (PREMIUM)"],
     [
        ["Bậc gói", "1", "2"],
        ["Giá theo tháng", "990.000 đ", "2.490.000 đ"],
        ["Giá theo quý", "2.700.000 đ", "6.900.000 đ"],
        ["Giá theo năm", "9.900.000 đ", "24.900.000 đ"],
        ["Gia Sư AI", "Không bao gồm", "Bao gồm"],
        ["Phòng học 3D", "Không bao gồm", "Bao gồm"],
        ["Hạn mức giáo viên", "20", "Không giới hạn"],
        ["Hạn mức học sinh / phụ huynh", "500 / 500", "Không giới hạn"],
        ["Hạn mức quản trị / kế toán / giám thị", "3 / 2 / 2", "Không giới hạn"],
        ["Hạn mức lớp học", "20", "Không giới hạn"],
     ],
     [6.0, 4.75, 4.75]),
    ("p", "Cách đặt giá theo ba chu kỳ tạo ra chiết khấu tự nhiên cho khách hàng cam kết dài hạn: giá năm của gói "
          "Cơ Bản tương đương mười tháng thuê bao, thay vì mười hai tháng nếu thanh toán theo tháng. Đây là kỹ "
          "thuật định giá phổ biến trong mô hình SaaS nhằm giảm tỷ lệ rời bỏ và cải thiện dòng tiền."),
    ("p", "Loại mặt hàng thứ hai là tính năng bán lẻ (add-on), dành cho trường đang dùng gói thấp nhưng chỉ cần "
          "một tính năng cao cấp cụ thể. Mỗi add-on gắn với đúng một tính năng cần mở khóa và cũng có ba mức giá "
          "theo chu kỳ, như trình bày trong Bảng 3.3."),
    ("tbl", "Danh mục tính năng bán lẻ (add-on)",
     ["Mã add-on", "Tên", "Giá tháng", "Giá quý", "Giá năm"],
     [
        ["AI_TUTOR", "Gia Sư AI", "790.000 đ", "2.100.000 đ", "7.900.000 đ"],
        ["VIRTUAL_LAB", "Phòng học 3D", "990.000 đ", "2.700.000 đ", "9.900.000 đ"],
     ],
     [3.2, 3.4, 2.9, 2.9, 3.1]),
    ("p", "Loại mặt hàng thứ ba là gói hạn mức bổ sung, cho phép trường mua thêm số lượng tài khoản theo vai trò "
          "hoặc số lớp học mà không phải nâng cấp gói. Hạn mức mua thêm được cộng dồn với hạn mức gốc của gói; "
          "trường hợp hạn mức gốc đã là không giới hạn thì phần mua thêm không có tác dụng."),
    ("p", "Toàn bộ ba danh mục trên do quản trị nền tảng vận hành qua màn hình quản trị catalog. Người quản trị "
          "có thể thêm mới, sửa giá, sửa hạn mức và bật hoặc tắt trạng thái kinh doanh của từng mặt hàng. Việc "
          "tắt một mặt hàng không xóa dữ liệu lịch sử: các trường đang dùng vẫn giữ nguyên quyền lợi tới hết kỳ, "
          "chỉ khách hàng mới không còn nhìn thấy mặt hàng đó trong catalog."),
    ("p", "Về phía tuyến B2C, “danh mục sản phẩm” chính là biểu phí học phí do kế toán từng trường cấu hình. Mỗi "
          "cấu hình biểu phí xác định loại khoản thu, số tiền, chu kỳ thu, ngày đến hạn trong tháng và phạm vi áp "
          "dụng theo năm học và khối lớp. Cấu hình không gắn khối lớp cụ thể sẽ áp dụng cho toàn trường."),
    ("p", "Điểm khác biệt cần nhấn mạnh so với thương mại điện tử hàng hóa là khái niệm tồn kho. Sản phẩm ở đây "
          "là quyền sử dụng phần mềm nên không có tồn kho vật lý; vai trò kiểm soát nguồn cung được thay bằng cơ "
          "chế hạn mức. Trước mỗi thao tác tạo tài khoản hoặc tạo lớp, hệ thống kiểm tra số lượng hiện có so với "
          "hạn mức khả dụng và từ chối nếu đã chạm trần, đồng thời gợi ý mua thêm hạn mức. Cơ chế này giữ vai trò "
          "tương đương việc kiểm tra tồn kho trước khi cho phép đặt hàng."),
    ("p", "Hình 3.4 minh họa màn hình danh mục gói dịch vụ mà quản trị trường nhìn thấy khi lựa chọn sản phẩm."),
    ("ph", "Màn hình danh mục gói dịch vụ và bảng giá theo chu kỳ", 7.5),
]

C3_2_2 = [
    ("p", "Do đặc thù sản phẩm là dịch vụ thuê bao, mỗi giao dịch thường chỉ gồm một tới vài dòng hàng và không "
          "cần giỏ hàng tồn tại lâu dài giữa các phiên làm việc như trang thương mại điện tử bán lẻ. Vì vậy hệ "
          "thống cài đặt giỏ hàng dưới dạng đơn hàng ở trạng thái chờ thanh toán: ngay khi người dùng chọn mua, "
          "hệ thống sinh một đơn hàng cùng các dòng chi tiết và chuyển thẳng sang bước xác nhận."),
    ("p", "Mỗi đơn hàng mang một mã đơn duy nhất, loại đơn, trạng thái, tổng tiền, thời điểm thanh toán và mã giao "
          "dịch do cổng thanh toán trả về. Bốn loại đơn được phân biệt rõ vì mỗi loại kéo theo một hệ quả nghiệp "
          "vụ khác nhau sau khi thanh toán thành công:"),
    ("b", [
        "Đơn đăng ký mới: kích hoạt thuê bao, đặt ngày bắt đầu và ngày kết thúc kỳ theo chu kỳ đã chọn.",
        "Đơn nâng cấp: chuyển sang gói có bậc cao hơn và cập nhật lại toàn bộ hạn mức cùng quyền dùng tính năng.",
        "Đơn mua add-on hoặc hạn mức bổ sung: gắn thêm quyền lợi vào thuê bao hiện hành, hiệu lực canh theo ngày "
        "kết thúc kỳ của gói nên không tạo ra kỳ hạn lệch nhau.",
        "Đơn gia hạn: đẩy ngày kết thúc kỳ thêm một chu kỳ mà không thay đổi gói.",
    ]),
    ("p", "Các dòng chi tiết đơn hàng lưu loại mặt hàng, mã tham chiếu tới mặt hàng gốc, đơn giá và thành tiền tại "
          "thời điểm mua. Việc chốt giá vào dòng đơn hàng thay vì tra ngược về catalog là bắt buộc: nếu quản trị "
          "nền tảng thay đổi bảng giá sau này, các đơn đã phát hành vẫn giữ nguyên giá trị lịch sử, bảo đảm tính "
          "chính xác cho báo cáo doanh thu và cho việc đối chiếu với khách hàng."),
    ("p", "Tại trang thanh toán, người mua vẫn có thể đổi chu kỳ của đơn trước khi trả tiền. Khi đó hệ thống tính "
          "lại đơn giá của toàn bộ dòng hàng theo chu kỳ mới và cập nhật tổng tiền. Thao tác này chỉ được phép "
          "khi đơn còn ở trạng thái chờ thanh toán; đơn đã thanh toán bị khóa để bảo toàn tính bất biến của chứng "
          "từ."),
    ("p", "Ở tuyến B2C, vai trò của đơn hàng do hóa đơn học phí đảm nhiệm. Kế toán chọn cấu hình biểu phí và kỳ "
          "thanh toán, hệ thống sinh hàng loạt hóa đơn cho toàn bộ học sinh thuộc phạm vi áp dụng. Mỗi hóa đơn "
          "gồm mã hóa đơn, kỳ thanh toán, số tiền gốc, khoản giảm trừ, số tiền phải trả, ngày đến hạn và trạng "
          "thái. Cách làm này thay thế thao tác lập từng phiếu thu thủ công vốn chiếm phần lớn thời gian của bộ "
          "phận kế toán trong mô hình cũ."),
    ("p", "Hình 3.5 minh họa màn hình quản lý hóa đơn học phí của kế toán, trong đó danh sách hóa đơn có thể lọc "
          "theo lớp, theo kỳ thanh toán và theo trạng thái thu."),
    ("ph", "Màn hình danh sách hóa đơn học phí và bộ lọc theo trạng thái", 7.5),
]

C3_2_3 = [
    ("p", "Xử lý giao dịch trực tuyến là phần nhạy cảm nhất của hệ thống thương mại điện tử, vì mọi sai sót đều "
          "quy đổi trực tiếp thành thiệt hại tài chính hoặc mất niềm tin của khách hàng. Hình 3.6 mô tả bảy bước "
          "của quy trình được cài đặt trong Lumina Tutors."),
    ("fig", "h3_7_oltp.png", "Quy trình xử lý giao dịch trực tuyến trong hệ thống", 15.5),
    ("p", "Quy trình bắt đầu từ việc người dùng chọn sản phẩm — một gói dịch vụ, một add-on hoặc một hóa đơn học "
          "phí. Hệ thống dựng các dòng chi tiết tương ứng, tính tổng tiền và sinh đơn hàng ở trạng thái chờ thanh "
          "toán. Người dùng chuyển sang cổng thanh toán; khi kết quả trả về hợp lệ, hệ thống kích hoạt quyền sử "
          "dụng, cập nhật trạng thái đơn thành đã thanh toán và gửi biên nhận."),
    ("p", "Ba nguyên tắc kỹ thuật được áp dụng để bảo đảm tính đúng đắn của quy trình."),
    ("p", "Nguyên tắc thứ nhất là tính nguyên tử của giao dịch. Việc ghi nhận thanh toán và việc cập nhật trạng "
          "thái thuê bao được thực hiện trong cùng một đơn vị công việc (Unit of Work), nên hoặc cả hai cùng "
          "thành công, hoặc cả hai cùng được hoàn tác. Trạng thái “đã thu tiền nhưng chưa cấp quyền” — vốn là "
          "nguồn khiếu nại phổ biến nhất của các hệ thống bán hàng trực tuyến — vì vậy không thể xảy ra do lỗi "
          "cập nhật một phần."),
    ("p", "Nguyên tắc thứ hai là tính idempotent. Cổng thanh toán có thể gửi thông báo kết quả nhiều lần do cơ "
          "chế thử lại khi chưa nhận được phản hồi, và người dùng cũng có thể tải lại trang kết quả. Hàm xác nhận "
          "thanh toán vì vậy kiểm tra trạng thái đơn trước khi xử lý: nếu đơn đã ở trạng thái đã thanh toán, hàm "
          "trả về kết quả thành công mà không thực hiện lại bất kỳ tác động phụ nào. Nhờ đó một lần trả tiền chỉ "
          "sinh đúng một lần cấp quyền, dù thông báo được gửi lặp bao nhiêu lần."),
    ("p", "Nguyên tắc thứ ba là bảo toàn dấu vết. Mỗi lần thanh toán đều lưu mã giao dịch của cổng và toàn bộ dữ "
          "liệu phản hồi gốc dưới dạng JSON. Đây là căn cứ để đối soát với sao kê của cổng thanh toán và là bằng "
          "chứng chống chối bỏ khi phát sinh tranh chấp."),
    ("p", "Vòng đời của thuê bao sau khi giao dịch hoàn tất được mô hình hóa bằng bốn trạng thái trong Hình 3.7. "
          "Trạng thái chờ thanh toán là điểm vào cho cả đăng ký lần đầu lẫn đăng ký lại sau khi hết hạn hoặc đã "
          "hủy; các nghiệp vụ gia hạn, nâng cấp và mua thêm đều là chuyển tiếp vòng lại trạng thái đang hiệu lực."),
    ("fig", "h3_5_vong_doi_goi.png", "Vòng đời trạng thái của thuê bao gói dịch vụ", 12.0),
    ("p", "Song song với vòng đời thuê bao, hai loại chứng từ giao dịch của hệ thống cũng có tập trạng thái riêng. "
          "Bảng 3.4 tổng hợp các trạng thái này cùng ý nghĩa nghiệp vụ tương ứng."),
    ("tbl", "Trạng thái đơn hàng và hóa đơn trong hệ thống",
     ["Đối tượng", "Trạng thái", "Ý nghĩa nghiệp vụ"],
     [
        ["Đơn gói dịch vụ", "Pending", "Đơn đã tạo, đang chờ thanh toán"],
        ["Đơn gói dịch vụ", "Paid", "Đã thanh toán, quyền lợi đã được kích hoạt"],
        ["Đơn gói dịch vụ", "Cancelled", "Đơn bị hủy trước khi thanh toán"],
        ["Đơn gói dịch vụ", "Failed", "Giao dịch thất bại tại cổng thanh toán"],
        ["Hóa đơn học phí", "Pending", "Đã phát hành, chưa tới hạn hoặc chưa thu"],
        ["Hóa đơn học phí", "Partial", "Đã thu một phần, còn dư nợ"],
        ["Hóa đơn học phí", "Paid", "Đã thu đủ"],
        ["Hóa đơn học phí", "Overdue", "Quá hạn thanh toán"],
        ["Hóa đơn học phí", "Cancelled", "Đã hủy do sai sót hoặc học sinh nghỉ học"],
     ],
     [4.0, 3.3, 8.2]),
]

C3_3_0 = [
    ("p", "Electronic Marketing là nhóm chức năng hỗ trợ thu hút, tương tác và giữ chân khách hàng. Vì Lumina "
          "Tutors phục vụ hai nhóm đối tượng khác nhau, các hoạt động tiếp thị cũng được tách thành hai lớp: lớp "
          "hướng tới nhà trường nhằm giới thiệu và thuyết phục mua gói dịch vụ, và lớp hướng tới phụ huynh – học "
          "sinh nhằm duy trì mức độ gắn kết với nền tảng trong suốt kỳ học."),
]

C3_3_1 = [
    ("p", "Quản lý nội dung trong hệ thống được cài đặt ở hai cấp độ."),
    ("p", "Cấp độ thứ nhất là trang giới thiệu công khai của nền tảng. Đây là điểm tiếp xúc đầu tiên với nhà "
          "trường đang cân nhắc lựa chọn phần mềm, trình bày định vị sản phẩm, các phân hệ chức năng và bảng giá "
          "gói dịch vụ. Trang này không yêu cầu đăng nhập và là trang duy nhất được thiết kế cho mục đích thu hút "
          "khách hàng mới."),
    ("p", "Cấp độ thứ hai là bảng tin nội bộ của từng trường. Mỗi trường có kho nội dung riêng để đăng thông báo, "
          "tin tức hoạt động và nội dung truyền thông tới học sinh và phụ huynh. Do hệ thống hoạt động theo mô "
          "hình đa đơn vị, toàn bộ nội dung được cách ly theo mã trường: người dùng của trường này không thể truy "
          "cập nội dung của trường khác dù biết chính xác định danh của bản ghi."),
    ("p", "Nội dung khóa học trực tuyến là loại nội dung số thứ ba, được tổ chức ba cấp gồm khóa học, chương và "
          "bài học. Giáo viên biên soạn khóa học một lần và tái sử dụng qua nhiều lớp, nhiều năm học. Mỗi khóa "
          "học có trạng thái bản nháp hoặc đã xuất bản, cho phép hoàn thiện nội dung trước khi hiển thị tới học "
          "sinh — cơ chế tương đương việc kiểm duyệt nội dung trước khi đăng của các hệ thống quản trị nội dung "
          "thương mại."),
    ("p", "Hình 3.8 minh họa trang giới thiệu công khai của nền tảng và khu vực bảng tin nội bộ của trường."),
    ("ph", "Trang giới thiệu công khai và khu vực bảng tin của trường", 7.5),
]

C3_3_2 = [
    ("p", "Nhóm chức năng tìm kiếm trong hệ thống được cài đặt ở dạng tìm kiếm và lọc nội bộ trên dữ liệu nghiệp "
          "vụ, thay vì một máy tìm kiếm toàn văn độc lập. Các màn hình danh sách khối lượng lớn — danh sách học "
          "sinh, danh sách lớp, danh sách hóa đơn, danh sách khóa học — đều hỗ trợ lọc theo nhiều tiêu chí kết "
          "hợp và phân trang phía máy chủ. Việc phân trang được thực hiện ở tầng cơ sở dữ liệu thay vì tải toàn "
          "bộ bản ghi rồi cắt ở bộ nhớ, nhằm giữ thời gian phản hồi ổn định khi dữ liệu tăng theo năm học."),
    ("p", "Về tối ưu hóa công cụ tìm kiếm, phần đã triển khai giới hạn ở các yếu tố tối ưu trên trang. Bố cục "
          "chung của ứng dụng khai báo bảng mã UTF-8, thẻ viewport phục vụ hiển thị đáp ứng trên thiết bị di "
          "động, thẻ mô tả trang và tiêu đề trang được sinh động theo từng khung nhìn thay vì cố định. Trang giới "
          "thiệu công khai có thẻ mô tả riêng phản ánh định vị sản phẩm."),
    ("p", "Cần nêu rõ giới hạn: hệ thống chưa phát sinh sơ đồ trang (sitemap), chưa có tệp khai báo cho robot tìm "
          "kiếm, chưa dùng đường dẫn thân thiện dạng ngữ nghĩa và chưa gắn dữ liệu có cấu trúc. Nguyên nhân là "
          "phần lớn màn hình của hệ thống nằm sau lớp xác thực, không phải là nội dung công khai để lập chỉ mục. "
          "Các hạng mục còn thiếu được ghi nhận trong phần hạn chế ở Mục 4.2 và định hướng bổ sung ở Mục 4.3."),
]

C3_3_3 = [
    ("p", "Cá nhân hóa bằng trí tuệ nhân tạo trong hệ thống được hiện thực hóa qua phân hệ Gia Sư AI — một trợ "
          "giảng hội thoại phục vụ riêng từng học sinh. Đây đồng thời là một mặt hàng trong catalog thương mại "
          "điện tử, nên phân hệ này minh họa rõ mối liên hệ giữa tính năng sản phẩm và cơ chế bán hàng."),
    ("p", "Về kỹ thuật, mô hình ngôn ngữ được chạy cục bộ thông qua Ollama với mô hình qwen2.5:7b thay vì gọi "
          "dịch vụ đám mây. Lựa chọn này xuất phát từ yêu cầu bảo vệ dữ liệu người học: toàn bộ câu hỏi và nội "
          "dung hội thoại của học sinh không rời khỏi hạ tầng của nhà trường. Đổi lại, hệ thống phải chấp nhận "
          "chi phí phần cứng và một dịch vụ khởi động trước để làm nóng mô hình, giảm độ trễ cho lượt hỏi đầu "
          "tiên."),
    ("p", "Về nghiệp vụ, mỗi phiên hội thoại được lưu kèm danh sách thông điệp và ba trường phục vụ kiểm duyệt: "
          "cờ đã xem xét, cờ bị đánh dấu bất thường và ghi chú của quản trị viên. Nhờ đó nhà trường có thể rà "
          "soát nội dung tương tác giữa học sinh và trợ giảng, phù hợp với yêu cầu quản lý đối tượng người dùng "
          "chưa thành niên."),
    ("p", "Về thương mại, quyền truy cập tính năng này được kiểm soát bằng cơ chế entitlement. Trước mỗi lần "
          "phục vụ, hệ thống kiểm tra trường hiện tại có gói bao gồm sẵn tính năng hay đã mua add-on tương ứng "
          "còn hiệu lực hay không. Đây chính là mô hình freemium có kiểm soát: tính năng tồn tại trong sản phẩm "
          "nhưng chỉ mở khóa khi khách hàng đã trả tiền, và tự động khóa lại khi kỳ thuê bao kết thúc mà không "
          "cần thao tác thủ công."),
    ("p", "Hình 3.9 minh họa giao diện hội thoại giữa học sinh và trợ giảng AI."),
    ("ph", "Giao diện hội thoại của phân hệ Gia Sư AI", 7.5),
]

C3_3_4 = [
    ("p", "Hệ thống cài đặt bốn kênh truyền thông tới khách hàng, phân biệt theo mức độ khẩn và theo đối tượng "
          "nhận."),
    ("p", "Kênh thứ nhất là thông báo trong ứng dụng. Người gửi chọn đối tượng nhận theo ba mức: toàn trường, "
          "theo khối lớp, theo lớp cụ thể hoặc chỉ định danh sách người dùng. Mỗi thông báo sinh ra các bản ghi "
          "người nhận riêng biệt, mỗi bản ghi lưu trạng thái đã đọc, thời điểm đọc và trạng thái chuyển phát. "
          "Nhờ tách bảng người nhận, hệ thống thống kê được tỷ lệ đọc — chỉ số cơ bản để đánh giá hiệu quả của "
          "một chiến dịch truyền thông. Thông báo còn hỗ trợ đặt lịch gửi vào thời điểm định trước thay vì gửi "
          "ngay."),
    ("p", "Kênh thứ hai là thư điện tử, hiện phục vụ nghiệp vụ gửi biên nhận và hóa đơn gói dịch vụ tới nhà "
          "trường sau khi thanh toán thành công. Nhà trường tự khai báo địa chỉ nhận hóa đơn; quản trị viên có "
          "thể gửi thư kiểm tra để xác nhận cấu hình máy chủ thư trước khi phát sinh giao dịch thật, và gửi lại "
          "biên nhận cho một đơn hàng bất kỳ khi cần. Nội dung thư được dựng ở cả hai định dạng HTML và văn bản "
          "thuần nhằm bảo đảm hiển thị đúng trên mọi ứng dụng thư. Trong môi trường phát triển, thư được ghi ra "
          "thư mục cục bộ thay vì gửi thật để tránh làm phiền người nhận trong quá trình kiểm thử."),
    ("p", "Kênh thứ ba là nhắn tin nội bộ giữa các thành viên trong trường, tổ chức theo cuộc hội thoại một–một "
          "hoặc theo nhóm, có hỗ trợ tệp đính kèm và mốc thời gian đọc cuối của từng người tham gia."),
    ("p", "Kênh thứ tư là chat hỗ trợ giữa nhà trường và quản trị nền tảng. Mỗi trường có một luồng hỗ trợ riêng; "
          "quản trị nền tảng thấy danh sách toàn bộ luồng kèm số tin nhắn chưa đọc. Đây là kênh chăm sóc khách "
          "hàng sau bán của tuyến B2B, đóng vai trò tương đương bộ phận hỗ trợ trực tuyến trên các sàn thương mại "
          "điện tử."),
]

C3_4_0 = [
    ("p", "Mục này trình bày cách hai mô hình giao dịch cùng tồn tại trong một nền tảng và các điểm tích hợp với "
          "dịch vụ bên ngoài."),
]

C3_4_1 = [
    ("p", "Tuyến B2C trong hệ thống là quan hệ giữa nhà trường và phụ huynh – học sinh. Chuỗi tương tác gồm các "
          "bước sau:"),
    ("b", [
        "Tài khoản phụ huynh và học sinh được nhà trường khởi tạo, hoặc tự đăng ký qua liên kết mời có thời hạn "
        "ba ngày. Mật khẩu được băm trước khi lưu, không có bất kỳ nơi nào trong hệ thống lưu mật khẩu ở dạng rõ.",
        "Phụ huynh đăng nhập và xem danh sách hóa đơn học phí của con em mình, gồm kỳ thanh toán, số tiền phải "
        "trả, ngày đến hạn và trạng thái.",
        "Phụ huynh chọn thanh toán trực tuyến; hệ thống chuyển hướng sang cổng VNPay và ghi nhận kết quả trả về.",
        "Trường hợp nộp tiền mặt hoặc chuyển khoản, kế toán ghi nhận thủ công và hệ thống lưu người xác nhận kèm "
        "thời điểm xác nhận.",
        "Phụ huynh nhận thông báo trong ứng dụng về kết quả học tập, chuyên cần, kỷ luật và các khoản thu sắp đến "
        "hạn.",
    ]),
    ("p", "Đặc điểm của tuyến này là số lượng giao dịch lớn nhưng giá trị mỗi giao dịch nhỏ và lặp lại theo chu "
          "kỳ cố định. Vì vậy yêu cầu thiết kế đặt trọng tâm vào việc phát hành hóa đơn hàng loạt, khả năng lọc "
          "nhanh theo trạng thái và theo lớp, cùng khả năng hỗ trợ thanh toán nhiều đợt cho một hóa đơn."),
]

C3_4_2 = [
    ("p", "Tuyến B2B là quan hệ giữa nền tảng Lumina Tutors và nhà trường với tư cách khách hàng doanh nghiệp. "
          "Chuỗi tương tác gồm các bước sau:"),
    ("b", [
        "Quản trị nền tảng khởi tạo tài khoản trường, gồm bản ghi trường và tài khoản quản trị đầu tiên của "
        "trường đó.",
        "Quản trị trường xem catalog gói dịch vụ, so sánh hạn mức và tính năng, chọn gói cùng chu kỳ thanh toán.",
        "Hệ thống sinh đơn hàng; trường thanh toán trực tuyến hoặc chuyển khoản rồi được xác nhận thủ công.",
        "Sau khi thanh toán thành công, thuê bao chuyển sang trạng thái đang hiệu lực, hạn mức và quyền dùng "
        "tính năng được áp dụng ngay.",
        "Trong kỳ, trường có thể nâng cấp gói, mua thêm tính năng lẻ hoặc mua thêm hạn mức khi chạm trần.",
        "Cuối kỳ, nếu cờ tự động gia hạn đang bật, tác vụ định kỳ sinh đơn gia hạn; nếu tắt, thuê bao chuyển sang "
        "trạng thái hết hạn.",
        "Trong suốt vòng đời, trường liên hệ với nền tảng qua luồng chat hỗ trợ riêng.",
    ]),
    ("p", "Điểm khác biệt căn bản so với tuyến B2C là ràng buộc hạn mức và cơ chế cách ly dữ liệu. Mỗi bản ghi "
          "thuộc phạm vi trường đều mang mã trường, và mọi truy vấn đều được lọc theo mã trường lấy từ thông tin "
          "định danh của người dùng đang đăng nhập. Nhờ đó dữ liệu của các trường được cách ly tuyệt đối dù cùng "
          "nằm trong một cơ sở dữ liệu vật lý — yêu cầu bắt buộc của mọi nền tảng SaaS đa khách hàng."),
    ("p", "Quản trị nền tảng có màn hình theo dõi toàn bộ thuê bao của các trường, danh sách đơn đã thanh toán và "
          "báo cáo doanh thu tổng hợp, đóng vai trò bảng điều khiển kinh doanh của tuyến B2B. Hình 3.10 minh họa "
          "màn hình này."),
    ("ph", "Màn hình quản lý thuê bao và đơn hàng của quản trị nền tảng", 7.5),
]

C3_4_3 = [
    ("p", "Hệ thống tích hợp cổng thanh toán VNPay cho cả hai tuyến giao dịch. VNPay được chọn vì hỗ trợ đầy đủ "
          "thẻ nội địa, ví điện tử và chuyển khoản qua ứng dụng ngân hàng — phù hợp với thói quen thanh toán của "
          "phụ huynh Việt Nam — đồng thời cung cấp môi trường thử nghiệm cho phép kiểm thử toàn bộ luồng mà không "
          "phát sinh giao dịch thật."),
    ("p", "Điểm quan trọng về mặt an toàn là hệ thống không tiếp nhận và không lưu trữ bất kỳ thông tin thẻ nào. "
          "Toàn bộ thao tác nhập thông tin thanh toán diễn ra trên trang của VNPay; hệ thống chỉ trao đổi với "
          "cổng các tham số đơn hàng đã được ký. Cách phân chia trách nhiệm này giúp giảm đáng kể phạm vi rủi ro "
          "phải bảo vệ."),
    ("p", "Hình 3.11 mô tả chi tiết mười bước của luồng thanh toán."),
    ("fig", "h3_4_luong_thanh_toan.png", "Luồng thanh toán trực tuyến qua cổng VNPay", 14.5),
    ("p", "Ở bước tạo yêu cầu, hệ thống dựng tập tham số theo đặc tả phiên bản 2.1.0 của VNPay và ký toàn bộ bằng "
          "thuật toán HMAC-SHA512 với khóa bí mật do VNPay cấp. Bảng 3.5 liệt kê các tham số chính và ý nghĩa "
          "nghiệp vụ của chúng."),
    ("tbl", "Các tham số chính trong yêu cầu thanh toán gửi tới VNPay",
     ["Tham số", "Ý nghĩa", "Cách hệ thống thiết lập"],
     [
        ["vnp_Version", "Phiên bản giao thức", "Cố định 2.1.0"],
        ["vnp_TmnCode", "Mã định danh đơn vị bán hàng", "Đọc từ tệp cấu hình, không lưu trong mã nguồn"],
        ["vnp_Amount", "Số tiền thanh toán", "Số tiền hóa đơn nhân 100 theo quy ước của cổng"],
        ["vnp_TxnRef", "Mã tham chiếu giao dịch", "Ghép mã hóa đơn với dấu thời gian để bảo đảm duy nhất trong ngày và ánh xạ ngược về hóa đơn"],
        ["vnp_OrderInfo", "Mô tả nội dung thanh toán", "Sinh tự động từ mã hóa đơn hoặc mã đơn hàng"],
        ["vnp_ReturnUrl", "Địa chỉ chuyển người dùng về", "Lấy từ cấu hình, nếu để trống thì dựng từ địa chỉ công khai của máy chủ"],
        ["vnp_CreateDate / vnp_ExpireDate", "Thời điểm tạo và hết hạn", "Theo giờ Việt Nam, hiệu lực 15 phút"],
        ["vnp_SecureHash", "Chữ ký toàn bộ tham số", "Ký bằng HMAC-SHA512 với khóa bí mật"],
     ],
     [4.4, 4.0, 7.1]),
    ("p", "Ở bước nhận kết quả, hệ thống xử lý hai đường phản hồi độc lập với vai trò khác nhau. Đường thứ nhất "
          "là thông báo tức thời (IPN) do máy chủ VNPay gọi trực tiếp tới máy chủ ứng dụng; đây là căn cứ duy "
          "nhất để cập nhật trạng thái đơn hàng. Đường thứ hai là chuyển hướng trình duyệt người dùng về trang "
          "kết quả; đường này chỉ dùng để hiển thị thông tin cho người dùng."),
    ("p", "Việc tách bạch hai đường phản hồi là biện pháp phòng vệ bắt buộc. Nếu cập nhật trạng thái đơn hàng dựa "
          "trên tham số của đường chuyển hướng, kẻ tấn công có thể tự dựng địa chỉ trả về với tham số thành công "
          "giả mạo. Ngược lại, thông báo tức thời đi thẳng giữa hai máy chủ và luôn được xác minh chữ ký trước khi "
          "xử lý. Ngoài chữ ký, hệ thống còn đối chiếu số tiền trong phản hồi với số tiền của hóa đơn để loại trừ "
          "trường hợp giá trị đơn hàng bị can thiệp."),
    ("p", "Hai luồng thanh toán của hai tuyến giao dịch dùng chung thư viện ký và kiểm tra chữ ký nhưng có địa "
          "chỉ tiếp nhận thông báo riêng, và mã tham chiếu giao dịch của tuyến B2B mang tiền tố riêng để hệ thống "
          "phân định đúng loại đơn khi nhận phản hồi."),
    ("p", "Hình 3.12 minh họa trang kết quả hiển thị cho người dùng sau khi hoàn tất giao dịch tại cổng thanh "
          "toán."),
    ("ph", "Trang kết quả giao dịch sau khi thanh toán qua VNPay", 7.5),
]

C3_4_4 = [
    ("p", "Sản phẩm của cả hai tuyến giao dịch đều là dịch vụ số, do đó khâu giao nhận không liên quan tới vận "
          "chuyển vật lý mà là quá trình bàn giao quyền truy cập. Hệ thống cài đặt ba hình thức bàn giao số."),
    ("p", "Hình thức thứ nhất là kích hoạt quyền sử dụng tính năng. Ngay khi thông báo thanh toán được xác minh, "
          "hệ thống cập nhật trạng thái thuê bao, gắn add-on hoặc hạn mức bổ sung vào kỳ hiện hành. Từ thời điểm "
          "đó, mọi lời gọi kiểm tra quyền đều trả về kết quả cho phép, và tính năng xuất hiện trong giao diện của "
          "toàn bộ người dùng thuộc trường. Thời gian bàn giao vì vậy gần như tức thời."),
    ("p", "Hình thức thứ hai là chuyển giao chứng từ điện tử. Biên nhận và hóa đơn gói dịch vụ được dựng thành "
          "thư điện tử và gửi tới địa chỉ nhận hóa đơn của trường ngay sau khi đơn chuyển sang trạng thái đã "
          "thanh toán. Quản trị viên có thể yêu cầu gửi lại chứng từ này bất cứ lúc nào."),
    ("p", "Hình thức thứ ba là bàn giao nội dung học tập. Khóa học trực tuyến được cấu trúc ba cấp và hỗ trợ hai "
          "cơ chế mở nội dung theo thời gian: mở sau một số ngày kể từ khi học sinh ghi danh, và mở từ một mốc "
          "thời gian tuyệt đối; nếu cấu hình cả hai thì phải thỏa mãn đồng thời. Khóa học cũng có thể đặt chế độ "
          "tuần tự, buộc học sinh hoàn thành bài trước mới mở bài sau. Đây chính là kỹ thuật nhỏ giọt nội dung "
          "(drip content) phổ biến của các nền tảng bán khóa học trực tuyến, vừa bảo vệ giá trị nội dung vừa duy "
          "trì nhịp học đều đặn."),
    ("p", "Tệp đính kèm phục vụ nội dung số được lưu trên hệ thống tệp của máy chủ với giới hạn dung lượng 50 MB "
          "cho mỗi tệp."),
]

C3_4_5 = [
    ("p", "Hệ thống cài đặt phân hệ chứng từ điện tử nội bộ phục vụ nghiệp vụ thu học phí và bán gói dịch vụ."),
    ("p", "Mỗi hóa đơn học phí mang mã hóa đơn duy nhất, kỳ thanh toán, số tiền gốc, khoản giảm trừ, số tiền phải "
          "trả được tính tại tầng ứng dụng, ngày đến hạn, trạng thái và trường dữ liệu dành cho mã QR tra cứu. "
          "Hóa đơn được sinh hàng loạt theo cấu hình biểu phí, giúp kế toán phát hành cho toàn bộ học sinh trong "
          "một thao tác thay vì lập từng phiếu."),
    ("p", "Quan hệ một–nhiều giữa hóa đơn và các lần thanh toán cho phép ghi nhận việc nộp thành nhiều đợt. Mỗi "
          "lần thanh toán lưu số tiền, thời điểm, phương thức, mã giao dịch, dữ liệu phản hồi gốc từ cổng và "
          "người xác nhận trong trường hợp thu ngoại tuyến. Trạng thái hóa đơn được suy ra từ tổng số tiền đã "
          "thu: chưa thu, thu một phần hoặc đã thu đủ."),
    ("p", "Đối với tuyến B2B, chứng từ được dựng dưới dạng thư điện tử có bố cục hóa đơn, liệt kê từng dòng hàng "
          "kèm đơn giá và thành tiền, thông tin trường mua và mã giao dịch. Nội dung được mã hóa ký tự đặc biệt "
          "trước khi ghép vào HTML nhằm loại trừ khả năng chèn mã độc qua tên trường hoặc mô tả sản phẩm."),
    ("p", "Cần nêu rõ giới hạn: đây là chứng từ nội bộ phục vụ đối chiếu giữa hai bên, chưa phải hóa đơn điện tử "
          "có chữ ký số theo quy định về hóa đơn của cơ quan thuế. Hệ thống chưa tích hợp với nhà cung cấp dịch "
          "vụ hóa đơn điện tử nào; hạng mục này được ghi nhận trong phần hạn chế tại Mục 4.2."),
]

C3_4_6 = [
    ("p", "Khả năng kết nối của hệ thống với thế giới bên ngoài được cài đặt qua bốn nhóm giao diện."),
    ("p", "Nhóm thứ nhất là REST API dành cho ứng dụng di động. Các điểm cuối nằm dưới tiền tố riêng, dùng cơ chế "
          "xác thực JWT thay vì cookie phiên, với thời hạn token truy cập 720 phút và token làm mới bảy ngày. "
          "Chính sách chia sẻ tài nguyên giữa các nguồn (CORS) được khai báo riêng cho ứng dụng di động. Nhờ "
          "tách bạch hai cơ chế xác thực, cùng một bộ nghiệp vụ phục vụ được cả trình duyệt lẫn thiết bị di động "
          "mà không phải sao chép logic."),
    ("p", "Nhóm thứ hai là giao tiếp thời gian thực qua SignalR trên nền WebSocket. Ba hub được đăng ký phục vụ "
          "lớp học trực tuyến, truyền dẫn âm thanh – hình ảnh và phòng thí nghiệm ảo. Đây là kênh đẩy dữ liệu từ "
          "máy chủ xuống trình duyệt mà không cần trình duyệt hỏi lại theo chu kỳ."),
    ("p", "Nhóm thứ ba là webhook tiếp nhận thông báo từ cổng thanh toán, đã trình bày ở Mục 3.4.3. Đây là điểm "
          "duy nhất hệ thống cho phép một máy chủ bên ngoài gọi vào mà không cần đăng nhập, và cũng vì vậy được "
          "bảo vệ bằng kiểm tra chữ ký thay cho cơ chế xác thực thông thường."),
    ("p", "Nhóm thứ tư là kết nối tới các dịch vụ nội bộ: máy chủ thư điện tử theo giao thức SMTP để gửi chứng "
          "từ, và dịch vụ mô hình ngôn ngữ chạy cục bộ phục vụ trợ giảng AI."),
    ("p", "Về khả năng phục vụ thị trường quốc tế, hệ thống hiện chỉ hỗ trợ tiếng Việt và đơn vị tiền tệ VND. "
          "Giao diện đa ngôn ngữ và đa tiền tệ chưa được triển khai; hạng mục này được ghi nhận tại Mục 4.2 và "
          "đề xuất trong định hướng phát triển ở Mục 4.3."),
]

C3_5_0 = [
    ("p", "Bảo mật của một hệ thống thương mại điện tử phải được xem xét trên bốn lớp: định danh người dùng, an "
          "toàn đường truyền và dữ liệu lưu trữ, an toàn ở tầng ứng dụng, và an toàn của bản thân giao dịch tài "
          "chính. Mục này trình bày các biện pháp đã được cài đặt trong hệ thống theo bốn lớp đó."),
]

C3_5_1 = [
    ("p", "Hệ thống dùng song song hai cơ chế xác thực cho hai loại máy khách. Giao diện web dùng xác thực dựa "
          "trên cookie phiên với thời hạn tám giờ và cơ chế gia hạn trượt, phù hợp với thói quen làm việc liên "
          "tục trong ngày của nhân sự nhà trường. Ứng dụng di động dùng JWT vì không có cơ chế cookie tự nhiên và "
          "cần lưu trữ chứng chỉ truy cập ở phía thiết bị."),
    ("p", "Mật khẩu được băm bằng thành phần băm mật khẩu chuẩn của nền tảng ASP.NET Core, sử dụng hàm dẫn xuất "
          "khóa có gắn muối ngẫu nhiên và lặp nhiều vòng. Hệ thống không lưu mật khẩu ở dạng rõ và không có bất "
          "kỳ chức năng nào cho phép đọc lại mật khẩu; nghiệp vụ khôi phục chỉ cho phép đặt lại mật khẩu mới."),
    ("p", "Phân quyền được cài đặt theo mô hình dựa trên vai trò, cụ thể hóa bằng chín chính sách truy cập gắn "
          "trực tiếp lên từng hành động của controller. Bảng 3.6 liệt kê các chính sách này."),
    ("tbl", "Các chính sách phân quyền của hệ thống",
     ["Chính sách", "Vai trò được phép", "Phạm vi áp dụng tiêu biểu"],
     [
        ["SystemAdmin", "SYSADMIN", "Quản trị catalog gói, khởi tạo trường, báo cáo doanh thu"],
        ["SchoolAdminOnly", "Quản trị trường", "Đăng ký, nâng cấp, gia hạn, hủy gói dịch vụ"],
        ["AdminOnly", "ADMIN", "Cấu hình hệ thống và nhân sự của trường"],
        ["TeacherOrAdmin", "TEACHER, ADMIN", "Quản lý lớp, khóa học, học sinh"],
        ["CourseAuthoring", "TEACHER", "Biên soạn, sửa và gán khóa học trực tuyến"],
        ["FinanceAccess", "ACCOUNTANT, ADMIN", "Biểu phí, hóa đơn học phí, công nợ"],
        ["SupervisorAccess", "SUPERVISOR, ADMIN", "Kỷ luật và thông báo tới phụ huynh"],
        ["LabAccess", "Sáu vai trò nghiệp vụ", "Phòng thí nghiệm ảo"],
        ["AnyAuthenticated", "Mọi tài khoản đã đăng nhập", "Thanh toán hóa đơn, xem thông tin cá nhân"],
     ],
     [3.9, 4.6, 6.8]),
    ("p", "Bên cạnh vai trò, mã trường được lưu trong thông tin định danh của phiên đăng nhập và được dùng làm "
          "điều kiện lọc bắt buộc cho mọi truy vấn dữ liệu thuộc phạm vi trường. Đây là lớp phòng vệ chống truy "
          "cập chéo giữa các khách hàng trong mô hình đa đơn vị."),
]

C3_5_2 = [
    ("p", "Trên môi trường triển khai thật, hệ thống bắt buộc chuyển hướng mọi kết nối HTTP sang HTTPS và bật cơ "
          "chế HSTS để trình duyệt ghi nhớ ràng buộc này cho các lần truy cập sau. Nhờ đó dữ liệu đăng nhập và "
          "thông tin giao dịch luôn được mã hóa trên đường truyền bằng TLS."),
    ("p", "Cookie xác thực được đánh dấu chỉ truy cập được từ máy chủ, ngăn mã JavaScript phía trình duyệt đọc "
          "được giá trị cookie ngay cả khi tồn tại lỗ hổng chèn mã. Cookie cũng được cấu hình chính sách gửi kèm "
          "theo nguồn nhằm hạn chế việc bị gửi đi trong các yêu cầu xuất phát từ trang bên thứ ba, và chính sách "
          "bảo mật được đồng bộ với giao thức của yêu cầu."),
    ("p", "Hệ thống bật xử lý các tiêu đề chuyển tiếp để hoạt động đúng khi đặt sau máy chủ trung gian: địa chỉ "
          "IP thật của người dùng, giao thức gốc và tên máy chủ gốc được khôi phục chính xác. Điều này đặc biệt "
          "quan trọng với luồng thanh toán, vì địa chỉ IP người mua là một tham số bắt buộc gửi tới cổng và địa "
          "chỉ trả về phải là địa chỉ công khai."),
    ("p", "Về dữ liệu lưu trữ, các bí mật gồm khóa ký của cổng thanh toán, mật khẩu máy chủ thư và chuỗi kết nối "
          "cơ sở dữ liệu được đặt trong tệp cấu hình theo môi trường, với khuyến nghị dùng cơ chế lưu bí mật của "
          "công cụ phát triển thay vì đưa vào mã nguồn. Nhật ký hệ thống được ghi bằng Serilog ra cả bảng điều "
          "khiển và tệp cuộn theo ngày, lưu trữ 30 ngày, phục vụ điều tra sự cố và đối soát giao dịch."),
]

C3_5_3 = [
    ("p", "Bảng 3.7 tổng hợp các biện pháp phòng vệ ở tầng ứng dụng đã được cài đặt, đối chiếu với nhóm rủi ro "
          "tương ứng."),
    ("tbl", "Biện pháp bảo mật ở tầng ứng dụng",
     ["Nhóm rủi ro", "Biện pháp đã cài đặt"],
     [
        ["Chèn mã SQL (SQL Injection)", "Toàn bộ truy vấn đi qua Entity Framework Core dưới dạng biểu thức LINQ; tham số được tách khỏi câu lệnh, không ghép chuỗi SQL thủ công"],
        ["Chèn mã kịch bản (XSS)", "Razor tự động mã hóa mọi dữ liệu xuất ra HTML; nội dung thư điện tử được mã hóa ký tự đặc biệt trước khi ghép vào khuôn mẫu"],
        ["Giả mạo yêu cầu (CSRF)", "Mọi hành động ghi dữ liệu đều yêu cầu token chống giả mạo, áp dụng nhất quán cho toàn bộ nghiệp vụ mua gói, gia hạn và hủy"],
        ["Dữ liệu đầu vào không hợp lệ", "Kiểm tra bằng FluentValidation ở tầng ứng dụng, kết hợp ràng buộc kiểu dữ liệu và ràng buộc ở tầng cơ sở dữ liệu"],
        ["Lạm dụng tài nguyên", "Cơ chế hạn mức kiểm tra trước mỗi thao tác tạo tài khoản hoặc tạo lớp; giới hạn dung lượng tệp tải lên 50 MB"],
        ["Rò rỉ thông tin qua trang lỗi", "Môi trường triển khai dùng trang lỗi chung, không hiển thị vết ngoại lệ; lỗi mất kết nối cơ sở dữ liệu được xử lý riêng bằng trang thông báo thân thiện"],
     ],
     [4.4, 10.9]),
    ("p", "Cần lưu ý một biện pháp chưa được cài đặt: hệ thống chưa có cơ chế giới hạn tần suất yêu cầu (rate "
          "limiting) trên các điểm cuối nhạy cảm như đăng nhập và tiếp nhận thông báo thanh toán. Đây là hạn chế "
          "được ghi nhận tại Mục 4.2."),
]

C3_5_4 = [
    ("p", "Bảo mật giao dịch là lớp phòng vệ cuối cùng và được cài đặt bằng bốn biện pháp bổ trợ lẫn nhau."),
    ("p", "Biện pháp thứ nhất là xác minh chữ ký. Mọi phản hồi từ cổng thanh toán đều được tính lại chữ ký "
          "HMAC-SHA512 từ tập tham số nhận được và so sánh với chữ ký đính kèm. Phản hồi không khớp chữ ký bị từ "
          "chối ngay, không tạo bất kỳ tác động nào lên dữ liệu."),
    ("p", "Biện pháp thứ hai là đối chiếu giá trị. Ngoài chữ ký, hệ thống so sánh số tiền trong phản hồi với số "
          "tiền của hóa đơn hoặc đơn hàng tương ứng. Sự chênh lệch dù nhỏ cũng khiến giao dịch bị từ chối và được "
          "ghi nhật ký để rà soát thủ công."),
    ("p", "Biện pháp thứ ba là chống xử lý lặp. Hàm xác nhận thanh toán kiểm tra trạng thái đơn trước khi tác "
          "động, nên việc cổng gửi lại thông báo nhiều lần hoặc người dùng tải lại trang không thể sinh ra hai "
          "lần cấp quyền hay hai bản ghi thu tiền cho cùng một giao dịch."),
    ("p", "Biện pháp thứ tư là lưu vết phục vụ chống chối bỏ. Mỗi bản ghi thanh toán lưu mã giao dịch của cổng, "
          "trạng thái và toàn bộ dữ liệu phản hồi gốc dưới dạng JSON. Kết hợp với nhật ký ứng dụng ghi lại thời "
          "điểm tạo yêu cầu và thời điểm nhận phản hồi, hệ thống có đủ căn cứ để đối soát với sao kê của cổng "
          "thanh toán và để giải quyết tranh chấp với khách hàng."),
]

C3_6_0 = [
    ("p", "Mục này trình bày mô hình triển khai hiện tại của hệ thống và đánh giá mức độ sẵn sàng cho việc mở "
          "rộng quy mô."),
]

C3_6_1 = [
    ("p", "Hệ thống được triển khai theo mô hình ba lớp vật lý như trong Hình 3.13: lớp máy khách gồm trình duyệt "
          "và ứng dụng di động, lớp máy chủ ứng dụng chạy tiến trình ASP.NET Core, và lớp dữ liệu gồm SQL Server "
          "cùng các dịch vụ nội bộ."),
    ("fig", "h3_6_trien_khai.png", "Mô hình triển khai của hệ thống", 15.5),
    ("p", "Trên một tiến trình máy chủ, ba giao diện cùng tồn tại: các controller MVC phục vụ trình duyệt qua xác "
          "thực cookie, nhóm API phục vụ ứng dụng di động qua xác thực JWT, và ba hub SignalR phục vụ các tính "
          "năng thời gian thực. Cách gộp này giữ cho việc triển khai đơn giản, phù hợp với quy mô một trường tới "
          "vài chục trường, đổi lại là ba nhóm tải khác nhau cùng chia sẻ một tiến trình."),
    ("p", "Dữ liệu nghiệp vụ nằm trong SQL Server; tệp đính kèm nằm trên hệ thống tệp của máy chủ ứng dụng; nhật "
          "ký được ghi ra tệp cuộn theo ngày với thời gian lưu trữ 30 ngày. Cấu trúc cơ sở dữ liệu được quản lý "
          "bằng cơ chế migration của Entity Framework Core, hiện có 16 phiên bản migration, cho phép nâng cấp "
          "lược đồ ở môi trường triển khai bằng một lệnh duy nhất mà không mất dữ liệu."),
    ("p", "Hai dịch vụ ngoài được kết nối là cổng thanh toán VNPay và máy chủ thư điện tử SMTP; một dịch vụ nội "
          "bộ là máy chủ mô hình ngôn ngữ Ollama chạy cục bộ."),
]

C3_6_2 = [
    ("p", "Hệ thống hiện được vận hành ở dạng tự quản trên máy chủ của nhà trường hoặc máy chủ phát triển, chưa "
          "triển khai lên hạ tầng đám mây công cộng."),
    ("p", "Tuy nhiên, một số hạng mục cấu hình đã được chuẩn bị sẵn cho tình huống đặt sau máy chủ trung gian "
          "hoặc đường hầm công khai — điều kiện cần khi đưa lên hạ tầng đám mây. Cụ thể, hệ thống xử lý các tiêu "
          "đề chuyển tiếp để khôi phục đúng giao thức và tên máy chủ gốc; địa chỉ trả về của cổng thanh toán được "
          "phép để trống và tự dựng từ địa chỉ công khai đang phục vụ. Nhờ vậy luồng thanh toán vẫn hoạt động "
          "chính xác khi địa chỉ công khai thay đổi giữa các lần triển khai."),
]

C3_6_3 = [
    ("p", "Hệ thống chưa triển khai cơ chế tự động co giãn. Mục này đánh giá mức độ sẵn sàng của kiến trúc hiện "
          "tại cho hạng mục đó, làm cơ sở cho định hướng phát triển ở Chương 4."),
    ("p", "Về phía thuận lợi, việc phân tách theo Clean Architecture giúp phần nghiệp vụ không giữ trạng thái "
          "riêng giữa các lần gọi; toàn bộ trạng thái nằm ở cơ sở dữ liệu. Truy cập dữ liệu đi qua một lớp trừu "
          "tượng thống nhất nên có thể bổ sung bộ nhớ đệm hoặc tách đọc – ghi mà không sửa tầng nghiệp vụ. Cơ chế "
          "cách ly theo mã trường cũng cho phép phân mảnh dữ liệu theo khách hàng khi quy mô tăng."),
    ("p", "Về phía rào cản, có ba điểm cần xử lý trước khi chạy nhiều bản sao ứng dụng. Thứ nhất, phiên làm việc "
          "hiện lưu trong bộ nhớ tiến trình nên cần chuyển sang kho phiên dùng chung. Thứ hai, SignalR chưa cấu "
          "hình thành phần điều phối giữa các bản sao nên kết nối thời gian thực sẽ không đồng bộ khi có nhiều "
          "máy chủ. Thứ ba, tệp đính kèm lưu trên đĩa cục bộ nên cần chuyển sang dịch vụ lưu trữ đối tượng dùng "
          "chung."),
]

C3_6_4 = [
    ("p", "Hệ thống chưa thiết lập quy trình tích hợp và triển khai liên tục tự động. Hoạt động xây dựng và kiểm "
          "thử hiện được thực hiện bằng lệnh trên máy phát triển, kết hợp một tệp kịch bản khởi chạy môi trường "
          "phát triển."),
    ("p", "Nền tảng cho việc tự động hóa về sau đã có sẵn ở phần kiểm thử. Giải pháp gồm hai dự án kiểm thử: dự "
          "án kiểm thử đơn vị dùng xUnit kết hợp thư viện giả lập Moq và thư viện khẳng định FluentAssertions, "
          "hướng vào tầng Application và tầng Domain; dự án kiểm thử tích hợp dùng xUnit kết hợp SpecFlow theo "
          "phong cách phát triển hướng hành vi, chạy trên cơ sở dữ liệu trong bộ nhớ thông qua nhà máy ứng dụng "
          "web, kèm kiểm thử giao diện bằng Selenium."),
    ("p", "Do bộ kiểm thử có thể chạy bằng một lệnh duy nhất và không phụ thuộc cơ sở dữ liệu ngoài, việc gắn vào "
          "một quy trình tự động sau này chỉ còn là công việc cấu hình. Cần lưu ý rằng kiểm thử tích hợp dùng cơ "
          "sở dữ liệu trong bộ nhớ nên không phản ánh được các hành vi đặc thù của SQL Server; đây là giới hạn "
          "cần tính đến khi đánh giá kết quả kiểm thử."),
]

# ═══════════════════════════════════════════════════════════════════════════════
#  CHƯƠNG 4 — KẾT LUẬN
# ═══════════════════════════════════════════════════════════════════════════════

C4_1 = [
    ("p", "Đề tài đã xây dựng hoàn chỉnh phân hệ thương mại điện tử cho nền tảng quản lý giáo dục Lumina Tutors, "
          "vận hành đồng thời hai tuyến giao dịch B2B và B2C trên một hạ tầng thanh toán thống nhất."),
    ("p", "Về mặt chức năng, các kết quả chính đạt được gồm:"),
    ("b", [
        "Danh mục sản phẩm số ba loại — gói dịch vụ, tính năng bán lẻ và hạn mức bổ sung — có định giá theo ba "
        "chu kỳ và cơ chế bật/tắt trạng thái kinh doanh, do quản trị nền tảng vận hành trực tiếp trên giao diện.",
        "Quy trình đặt hàng và thanh toán hoàn chỉnh cho bốn loại nghiệp vụ: đăng ký mới, nâng cấp gói, mua thêm "
        "và gia hạn; giá được chốt vào dòng đơn hàng bảo đảm tính bất biến của chứng từ.",
        "Tích hợp cổng thanh toán VNPay cho cả hai tuyến giao dịch, có tách bạch đường thông báo tức thời và "
        "đường chuyển hướng người dùng, xác minh chữ ký HMAC-SHA512 và đối chiếu số tiền.",
        "Cơ chế bàn giao số tức thời: kích hoạt quyền dùng tính năng ngay sau khi xác nhận thanh toán và gửi biên "
        "nhận qua thư điện tử.",
        "Cơ chế hạn mức đóng vai trò kiểm soát nguồn cung thay cho tồn kho, kiểm tra trước mỗi thao tác tạo tài "
        "khoản hoặc tạo lớp học.",
        "Phân hệ chứng từ học phí với phát hành hàng loạt, hỗ trợ thanh toán nhiều đợt và theo dõi công nợ.",
        "Bốn kênh truyền thông tới khách hàng: thông báo trong ứng dụng có phân nhóm đối tượng nhận, thư điện tử, "
        "nhắn tin nội bộ và chat hỗ trợ sau bán.",
        "Cá nhân hóa bằng trợ giảng AI chạy cục bộ, có kiểm duyệt nội dung và được kiểm soát truy cập theo quyền "
        "sử dụng đã mua.",
        "Bốn lớp biện pháp bảo mật phủ từ xác thực – phân quyền tới an toàn giao dịch, trong đó có cơ chế chống "
        "xử lý lặp và lưu vết chống chối bỏ.",
    ]),
    ("p", "Về mặt kỹ thuật, Bảng 4.1 tổng hợp khối lượng thành phần của toàn hệ thống tại thời điểm hoàn thành "
          "báo cáo."),
    ("tbl", "Thống kê khối lượng thành phần của hệ thống",
     ["Thành phần", "Số lượng", "Ghi chú"],
     [
        ["Lớp thực thể (Domain)", "85", "Tổ chức theo phân hệ nghiệp vụ"],
        ["Bảng dữ liệu (DbSet)", "85", "Ánh xạ một–một với lớp thực thể"],
        ["Dịch vụ nghiệp vụ (Application)", "25", "Mỗi dịch vụ phụ trách một phân hệ"],
        ["Controller (Web)", "32", "Bao gồm nhóm API cho ứng dụng di động"],
        ["Khung nhìn Razor", "136", "Giao diện web của toàn hệ thống"],
        ["Hub thời gian thực", "3", "Lớp học trực tuyến, truyền dẫn A/V, phòng thí nghiệm ảo"],
        ["Phiên bản migration", "16", "Lịch sử tiến hóa lược đồ cơ sở dữ liệu"],
        ["Chính sách phân quyền", "9", "Gắn trực tiếp lên hành động của controller"],
        ["Tổng số dòng mã C#", "Khoảng 127.000", "Không tính tệp sinh tự động"],
     ],
     [6.0, 3.0, 6.5]),
    ("p", "Về mặt học thuật, đề tài cho thấy một hệ thống bán sản phẩm số vẫn tuân thủ đầy đủ khung lý thuyết "
          "thương mại điện tử đã trình bày ở Chương 2, nhưng ba thành phần được diễn giải lại theo đặc thù sản "
          "phẩm: tồn kho trở thành hạn mức sử dụng, giao nhận trở thành cấp quyền truy cập, và doanh thu đơn lẻ "
          "trở thành doanh thu định kỳ có vòng đời riêng."),
]

C4_2 = [
    ("p", "Nhóm ghi nhận các hạn chế sau của hệ thống ở thời điểm kết thúc đồ án."),
    ("p", "Về phạm vi thanh toán, hệ thống mới tích hợp một cổng thanh toán duy nhất là VNPay và đang chạy ở môi "
          "trường thử nghiệm. Các cổng phổ biến khác như MoMo, ZaloPay và các cổng quốc tế chưa được tích hợp, "
          "khiến khách hàng không có lựa chọn thay thế khi cổng hiện tại gián đoạn."),
    ("p", "Về chứng từ, hệ thống mới dừng ở chứng từ nội bộ dạng thư điện tử, chưa tích hợp dịch vụ hóa đơn điện "
          "tử có chữ ký số theo quy định về hóa đơn. Nhà trường vì vậy vẫn phải phát hành hóa đơn thuế trên một "
          "hệ thống riêng."),
    ("p", "Về tiếp thị điện tử, các hạng mục tối ưu hóa công cụ tìm kiếm ngoài trang chưa được triển khai: chưa "
          "có sơ đồ trang, chưa có tệp khai báo cho robot tìm kiếm, chưa dùng đường dẫn thân thiện dạng ngữ nghĩa "
          "và chưa gắn dữ liệu có cấu trúc. Hệ thống cũng chưa có công cụ gửi thư tiếp thị hàng loạt theo chiến "
          "dịch, chưa có thông báo đẩy tới thiết bị di động và chưa có tính năng chia sẻ nội dung lên mạng xã "
          "hội."),
    ("p", "Về bảo mật, hệ thống chưa có cơ chế giới hạn tần suất yêu cầu trên các điểm cuối nhạy cảm, chưa bổ "
          "sung các tiêu đề bảo mật ở tầng phản hồi như chính sách bảo mật nội dung, và chưa hỗ trợ xác thực hai "
          "yếu tố cho các vai trò có quyền tài chính. Ngoài ra, khóa bí mật dùng cho JWT trong tệp cấu hình mẫu "
          "vẫn là giá trị giữ chỗ, bắt buộc phải thay thế trước khi vận hành thật."),
    ("p", "Về triển khai, hệ thống chưa được đưa lên hạ tầng đám mây, chưa có cơ chế tự động co giãn và chưa "
          "thiết lập quy trình tích hợp – triển khai liên tục. Ba rào cản kỹ thuật đã nêu ở Mục 3.6.3 — phiên làm "
          "việc lưu trong bộ nhớ tiến trình, SignalR chưa có thành phần điều phối, và tệp đính kèm lưu trên đĩa "
          "cục bộ — cần được xử lý trước khi chạy nhiều bản sao ứng dụng."),
    ("p", "Về khả năng phục vụ quốc tế, hệ thống chỉ hỗ trợ tiếng Việt và đơn vị tiền tệ VND."),
    ("p", "Về kiểm thử, bộ kiểm thử tích hợp chạy trên cơ sở dữ liệu trong bộ nhớ nên không phát hiện được các "
          "khác biệt hành vi so với SQL Server, đặc biệt ở các truy vấn phức tạp và các ràng buộc ở tầng cơ sở "
          "dữ liệu."),
]

C4_3 = [
    ("p", "Trên cơ sở các hạn chế đã nêu, nhóm đề xuất lộ trình phát triển theo ba giai đoạn, sắp xếp theo mức độ "
          "ưu tiên giảm dần."),
    ("p", "Giai đoạn thứ nhất tập trung hoàn thiện năng lực giao dịch. Nội dung gồm: trừu tượng hóa lớp cổng "
          "thanh toán thành một giao diện chung để bổ sung MoMo và ZaloPay mà không sửa tầng nghiệp vụ; tích hợp "
          "nhà cung cấp hóa đơn điện tử có chữ ký số; bổ sung cơ chế giới hạn tần suất yêu cầu và xác thực hai "
          "yếu tố cho các vai trò có quyền tài chính; xây dựng màn hình đối soát tự động giữa nhật ký giao dịch "
          "của hệ thống và sao kê của cổng thanh toán."),
    ("p", "Giai đoạn thứ hai tập trung nâng cao năng lực tiếp thị và giữ chân khách hàng. Nội dung gồm: bổ sung "
          "sơ đồ trang, tệp khai báo cho robot tìm kiếm, đường dẫn thân thiện và dữ liệu có cấu trúc cho các "
          "trang công khai; xây dựng công cụ gửi thư theo chiến dịch có thống kê tỷ lệ mở và tỷ lệ nhấp; bổ sung "
          "thông báo đẩy tới ứng dụng di động; mở rộng phân hệ cá nhân hóa từ trợ giảng AI sang gợi ý khóa học và "
          "gợi ý nội dung dựa trên lịch sử học tập."),
    ("p", "Giai đoạn thứ ba tập trung vào khả năng mở rộng vận hành. Nội dung gồm: chuyển phiên làm việc sang kho "
          "dùng chung, bổ sung thành phần điều phối cho SignalR và chuyển tệp đính kèm sang dịch vụ lưu trữ đối "
          "tượng; đóng gói ứng dụng thành ảnh chứa (container) và triển khai lên hạ tầng đám mây có tự động co "
          "giãn; thiết lập quy trình tích hợp – triển khai liên tục với các bước xây dựng, kiểm thử, triển khai "
          "và quay lui tự động; bổ sung hỗ trợ đa ngôn ngữ và đa tiền tệ để mở rộng ra thị trường khu vực."),
    ("p", "Ngoài ba giai đoạn trên, một hướng mở rộng đáng cân nhắc về mặt mô hình kinh doanh là kích hoạt kênh "
          "bán khóa học trực tuyến trực tiếp tới người học. Cấu trúc dữ liệu hiện tại đã dự trù sẵn hình thức ghi "
          "danh lẻ bên cạnh ghi danh theo lớp, nên việc bổ sung tuyến bán hàng này chủ yếu là xây dựng giao diện "
          "cửa hàng và gắn vào luồng thanh toán sẵn có."),
]

C4_4 = [
    ("p", "Bảng 4.2 trình bày phân công công việc giữa các thành viên trong nhóm."),
]

PHAN_CONG = [
    ["1", "Nguyễn Trần Thanh Nhã\n22DH112486",
     "Phân tích nghiệp vụ và thiết kế cơ sở dữ liệu phân hệ thương mại điện tử; xây dựng danh mục gói dịch vụ, "
     "add-on và hạn mức bổ sung; cài đặt quy trình đặt hàng, gia hạn và cơ chế quyền sử dụng tính năng; soạn "
     "Chương 1, Chương 2 và Mục 3.1 – 3.3 của báo cáo."],
    ["2", "Nguyễn Anh Tuấn\n22DH114065",
     "Tích hợp cổng thanh toán VNPay cho cả hai tuyến giao dịch; xây dựng phân hệ hóa đơn học phí và gửi chứng "
     "từ qua thư điện tử; cài đặt các biện pháp bảo mật ứng dụng và bảo mật giao dịch; kiểm thử luồng thanh "
     "toán; soạn Mục 3.4 – 3.6 và Chương 4 của báo cáo."],
]

# ═══════════════════════════════════════════════════════════════════════════════
#  TÀI LIỆU THAM KHẢO (chuẩn IEEE)
# ═══════════════════════════════════════════════════════════════════════════════

TAI_LIEU = [
    "K. C. Laudon and C. G. Traver, E-commerce 2023–2024: Business, Technology, Society, 17th ed. Harlow, "
    "United Kingdom: Pearson Education, 2023.",

    "E. Turban, J. Outland, D. King, J. K. Lee, T.-P. Liang, and D. C. Turban, Electronic Commerce 2018: A "
    "Managerial and Social Networks Perspective, 9th ed. Cham, Switzerland: Springer, 2018.",

    "Organisation for Economic Co-operation and Development, OECD Guide to Measuring the Information Society "
    "2011. Paris, France: OECD Publishing, 2011.",

    "Quốc hội nước Cộng hòa xã hội chủ nghĩa Việt Nam, “Luật Giao dịch điện tử,” Luật số 20/2023/QH15, ngày 22 "
    "tháng 6 năm 2023, có hiệu lực từ ngày 01 tháng 7 năm 2024.",

    "Chính phủ nước Cộng hòa xã hội chủ nghĩa Việt Nam, “Nghị định về thương mại điện tử,” Nghị định số "
    "52/2013/NĐ-CP, ngày 16 tháng 5 năm 2013.",

    "Chính phủ nước Cộng hòa xã hội chủ nghĩa Việt Nam, “Nghị định sửa đổi, bổ sung một số điều của Nghị định số "
    "52/2013/NĐ-CP ngày 16 tháng 5 năm 2013 của Chính phủ về thương mại điện tử,” Nghị định số 85/2021/NĐ-CP, "
    "ngày 25 tháng 9 năm 2021.",

    "Bộ Giáo dục và Đào tạo, “Thông tư quy định về đánh giá học sinh trung học cơ sở và học sinh trung học phổ "
    "thông,” Thông tư số 22/2021/TT-BGDĐT, ngày 20 tháng 7 năm 2021.",

    "Hiệp hội Thương mại điện tử Việt Nam (VECOM), Báo cáo Chỉ số Thương mại điện tử Việt Nam 2025. Hà Nội, Việt "
    "Nam: VECOM, 2025. [Trực tuyến]. Địa chỉ: https://vecom.vn/bao-cao-chi-so-thuong-mai-dien-tu-viet-nam-2025",

    "R. C. Martin, Clean Architecture: A Craftsman's Guide to Software Structure and Design. Boston, MA, USA: "
    "Prentice Hall, 2018.",

    "M. Fowler, Patterns of Enterprise Application Architecture. Boston, MA, USA: Addison-Wesley, 2003.",

    "Microsoft Corporation, “ASP.NET Core documentation.” [Trực tuyến]. Địa chỉ: "
    "https://learn.microsoft.com/aspnet/core. [Truy cập: 20-07-2026].",

    "Microsoft Corporation, “Entity Framework Core documentation.” [Trực tuyến]. Địa chỉ: "
    "https://learn.microsoft.com/ef/core. [Truy cập: 20-07-2026].",

    "Công ty Cổ phần Giải pháp Thanh toán Việt Nam (VNPAY), “Tài liệu tích hợp cổng thanh toán VNPAY-QR, phiên "
    "bản 2.1.0.” [Trực tuyến]. Địa chỉ: https://sandbox.vnpayment.vn/apis. [Truy cập: 15-07-2026].",

    "H. Krawczyk, M. Bellare, and R. Canetti, “HMAC: Keyed-Hashing for Message Authentication,” Internet "
    "Engineering Task Force, RFC 2104, Feb. 1997.",

    "M. Jones, J. Bradley, and N. Sakimura, “JSON Web Token (JWT),” Internet Engineering Task Force, RFC 7519, "
    "May 2015.",

    "OWASP Foundation, OWASP Top 10:2021 – The Ten Most Critical Web Application Security Risks. [Trực tuyến]. "
    "Địa chỉ: https://owasp.org/Top10. [Truy cập: 10-07-2026].",
]
