# -*- coding: utf-8 -*-
"""Sinh các sơ đồ minh họa cho báo cáo Thương mại điện tử — Lumina Tutors.

Mỗi sơ đồ được kiểm tra cỡ chữ hiệu dụng sau khi thu nhỏ về bề rộng đặt trong
báo cáo; mục tiêu là không nhỏ hơn 9 pt để bảo đảm đọc được khi in.
"""
import os
import subprocess

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "hinh-anh")
os.makedirs(OUT, exist_ok=True)

FONT = "Liberation Serif"   # tương thích số đo với Times New Roman
INK, LINE, FILL1, FILL2, FILL3, ACC = "#1a1a1a", "#555555", "#f4f6f9", "#e8eef7", "#fdf6e3", "#b8860b"
DPI = 220

kiem_tra = []


def render(name, src, font_co_ban, rong_dat_cm, fmt="png"):
    path = os.path.join(OUT, f"{name}.{fmt}")
    p = subprocess.run(["dot", f"-T{fmt}", f"-Gdpi={DPI}", "-o", path],
                       input=src.encode("utf-8"), capture_output=True)
    if p.returncode != 0:
        raise RuntimeError(f"{name}: {p.stderr.decode()}")
    from PIL import Image
    w, h = Image.open(path).size
    rong_do_hoa_cm = w / DPI * 2.54
    ty_le = rong_dat_cm / rong_do_hoa_cm
    kiem_tra.append((name, f"{w}x{h}", round(w / h, 2), round(font_co_ban * ty_le, 1),
                     round(h / DPI * 2.54 * ty_le, 1)))


# ── Hình 3.1 — Kiến trúc Clean Architecture 4 tầng ───────────────────────────
render("h3_1_kien_truc", f"""
digraph G {{
  rankdir=TB; bgcolor="white"; splines=ortho; nodesep=0.35; ranksep=0.42;
  node [shape=box, style="filled,rounded", fontname="{FONT}", fontsize=15,
        color="{LINE}", fontcolor="{INK}", width=5.4, height=0.78, penwidth=1.2];
  edge [color="{LINE}", fontname="{FONT}", fontsize=12, penwidth=1.1];

  web  [label=<<b>Tầng Web</b> — ASP.NET Core 8 MVC<br/><font point-size="12">32 Controller · 136 View Razor · SignalR Hub · REST API (JWT)</font>>, fillcolor="{FILL2}"];
  infra[label=<<b>Tầng Infrastructure</b><br/><font point-size="12">EF Core 8 · DbContext (85 DbSet) · Repository · UnitOfWork · VNPay · SMTP</font>>, fillcolor="{FILL1}"];
  app  [label=<<b>Tầng Application</b><br/><font point-size="12">25 Service nghiệp vụ · DTO · AutoMapper · FluentValidation · Result&lt;T&gt;</font>>, fillcolor="{FILL1}"];
  dom  [label=<<b>Tầng Domain</b><br/><font point-size="12">85 Entity · Enum · IRepository&lt;T&gt; · IUnitOfWork — không phụ thuộc thư viện ngoài</font>>, fillcolor="{FILL3}"];

  web -> infra [label="  phụ thuộc"];
  infra -> app [label="  phụ thuộc"];
  app -> dom   [label="  phụ thuộc"];
}}
""", 15, 13.0)

# ── Hình 3.2 — Use Case tổng quan ────────────────────────────────────────────
render("h3_2_usecase", f"""
digraph UC {{
  rankdir=LR; bgcolor="white"; nodesep=0.28; ranksep=0.95; newrank=true; ordering=out;
  node [fontname="{FONT}", fontsize=14, color="{LINE}", fontcolor="{INK}"];
  edge [color="{LINE}", penwidth=1.0, arrowsize=0.7];

  sysadmin [shape=box, style="filled,rounded", fillcolor="{FILL2}", label="Quản trị\\nnền tảng", height=0.62, width=1.5];
  khach    [shape=box, style="filled,rounded", fillcolor="{FILL2}", label="Nhà trường\\n(khách hàng B2B)", height=0.62, width=1.5];
  phuhuynh [shape=box, style="filled,rounded", fillcolor="{FILL2}", label="Phụ huynh /\\nHọc sinh (B2C)", height=0.62, width=1.5];

  subgraph cluster_sell {{
    label=<<b>Electronic Selling</b>>; fontname="{FONT}"; fontsize=16; labelloc=t;
    style="rounded"; color="{ACC}"; penwidth=1.6; bgcolor="#fffdf6";
    uc0 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Quản trị\\ncatalog"];
    uc1 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Xem catalog\\ngói"];
    uc2 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Đăng ký /\\nnâng cấp gói"];
    uc3 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Mua add-on,\\nquota"];
    uc5 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Gia hạn gói"];
    uc6 [shape=ellipse, style=filled, fillcolor="{FILL1}", label="Thanh toán\\nhọc phí"];
    uc4 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Thanh toán\\ntrực tuyến"];
  }}

  subgraph cluster_mkt {{
    label=<<b>Electronic Marketing</b>>; fontname="{FONT}"; fontsize=16; labelloc=t;
    style="rounded"; color="{ACC}"; penwidth=1.6; bgcolor="#fffdf6";
    um4 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Chat hỗ trợ"];
    um1 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Trang giới thiệu\\n& bảng giá"];
    um2 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Bảng tin\\ncủa trường"];
    um3 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Thông báo,\\nemail"];
    um5 [shape=ellipse, style=filled, fillcolor="{FILL3}", label="Gia Sư AI"];
  }}

  sysadmin -> uc0;
  sysadmin -> um4;
  khach -> uc1; khach -> uc2; khach -> uc3; khach -> uc5;
  khach -> um1 [style=dashed]; khach -> um4 [style=dashed];
  phuhuynh -> uc6;
  phuhuynh -> um2 [style=dashed]; phuhuynh -> um3 [style=dashed]; phuhuynh -> um5 [style=dashed];

  uc2 -> uc4 [style=dashed, label="«include»", fontsize=12];
  uc3 -> uc4 [style=dashed, label="«include»", fontsize=12];
  uc5 -> uc4 [style=dashed, label="«include»", fontsize=12];
  uc6 -> uc4 [style=dashed, label="«include»", fontsize=12];

  {{rank=same; sysadmin; khach; phuhuynh;}}
  {{rank=same; uc0; uc1; uc2; uc3; uc5; uc6; um4; um1; um2; um3; um5;}}
}}
""", 14, 14.5)


# ── Hình 3.3 — ERD phân hệ Thương mại điện tử ────────────────────────────────
def ent(name, title, rows, fill):
    body = "".join(
        f'<tr><td align="left"><font point-size="13">{r}</font></td></tr>' for r in rows)
    return (f'{name} [shape=plaintext, label=<<table border="1" cellborder="0" cellspacing="0" '
            f'cellpadding="4" bgcolor="white" color="{LINE}">'
            f'<tr><td bgcolor="{fill}"><b>{title}</b></td></tr>{body}</table>>];')


render("h3_3_erd", f"""
digraph ERD {{
  rankdir=TB; bgcolor="white"; nodesep=0.34; ranksep=0.55; splines=spline;
  node [fontname="{FONT}", fontsize=15, fontcolor="{INK}"];
  edge [color="{LINE}", fontname="{FONT}", fontsize=13, penwidth=1.0, arrowhead=crow, dir=both];

  {ent("plan", "SubscriptionPlan", ["PK Id", "PlanCode · Tier", "3 mức giá", "Cờ tính năng", "7 hạn mức"], FILL2)}
  {ent("addon", "SubscriptionAddOn", ["PK Id", "AddOnCode", "Feature · 3 mức giá"], FILL2)}
  {ent("school", "School", ["PK Id", "SchoolName", "BillingEmail"], FILL1)}
  {ent("sub", "SchoolSubscription", ["PK Id", "FK SchoolId · PlanId", "Status · BillingCycle", "CurrentPeriodEnd", "AutoRenew"], FILL3)}
  {ent("order", "SubscriptionOrder", ["PK Id", "OrderCode · OrderType", "Status · TotalAmount", "PaidAt · TransactionCode"], FILL3)}
  {ent("item", "SubscriptionOrderItem", ["PK Id", "ItemType · RefId", "UnitPrice · Amount"], FILL1)}
  {ent("cfg", "TuitionFeeConfig", ["PK Id", "FeeType · Amount", "BillingCycle · DueDay"], FILL2)}
  {ent("user", "User (Học sinh)", ["PK Id", "FullName · Email"], FILL1)}
  {ent("inv", "TuitionInvoice", ["PK Id", "InvoiceCode", "BillingPeriod", "Amount · Discount", "Status · DueDate"], FILL3)}
  {ent("pay", "TuitionPayment", ["PK Id", "PaymentMethod", "PaymentStatus", "TransactionCode"], FILL1)}

  plan   -> sub   [label=" 1 : N "];
  school -> sub   [label=" 1 : 1 "];
  sub    -> order [label=" 1 : N "];
  order  -> item  [label=" 1 : N "];
  addon  -> item  [label=" 1 : N ", style=dashed];
  school -> cfg   [label=" 1 : N "];
  cfg    -> inv   [label=" 1 : N "];
  user   -> inv   [label=" 1 : N "];
  inv    -> pay   [label=" 1 : N "];

  {{rank=same; plan; school; addon;}}
  {{rank=same; sub; cfg; user;}}
  {{rank=same; order; inv;}}
  {{rank=same; item; pay;}}
}}
""", 13, 15.5)

# ── Hình 3.4 (trong báo cáo là Hình 3.11) — Luồng thanh toán VNPay ───────────
render("h3_4_luong_thanh_toan", f"""
digraph SEQ {{
  rankdir=TB; bgcolor="white"; nodesep=0.25; ranksep=0.26; splines=ortho;
  node [shape=box, style="filled,rounded", fontname="{FONT}", fontsize=14,
        color="{LINE}", fontcolor="{INK}", width=3.9, height=0.52, penwidth=1.2];
  edge [color="{LINE}", fontname="{FONT}", fontsize=12, penwidth=1.1];

  s1 [label="1. Người dùng chọn hóa đơn / đơn hàng", fillcolor="{FILL2}"];
  s2 [label="2. Dựng tham số vnp_* và ký HMAC-SHA512", fillcolor="{FILL1}"];
  s3 [label="3. Chuyển hướng sang cổng VNPay (hạn 15 phút)", fillcolor="{FILL1}"];
  s4 [label="4. Người dùng xác thực và thanh toán tại VNPay", fillcolor="{FILL3}"];
  s5 [label="5. VNPay gọi IPN tới máy chủ ứng dụng", fillcolor="{FILL1}"];
  s6 [label="6. Xác minh chữ ký và đối chiếu số tiền", fillcolor="{FILL1}"];
  s7 [label="7. Ghi nhận thanh toán, cập nhật trạng thái\\n(xử lý idempotent)", fillcolor="{FILL2}"];
  s8 [label="8. Gửi biên nhận và thông báo", fillcolor="{FILL1}"];
  s9 [label="9. VNPay chuyển người dùng về trang kết quả", fillcolor="{FILL3}"];
  s10[label="10. Hiển thị kết quả giao dịch", fillcolor="{FILL2}"];

  s1->s2->s3->s4->s5->s6->s7->s8;
  s4->s9 [style=dashed, constraint=false, label="  luồng\\n  người dùng"];
  s9->s10 [style=dashed];
  s8->s10 [style=invis];
}}
""", 14, 14.5)

# ── Hình 3.5 (trong báo cáo là Hình 3.7) — Vòng đời gói dịch vụ ──────────────
render("h3_5_vong_doi_goi", f"""
digraph LIFE {{
  rankdir=TB; bgcolor="white"; nodesep=0.9; ranksep=1.1;
  node [shape=box, style="filled,rounded", fontname="{FONT}", fontsize=14,
        color="{LINE}", fontcolor="{INK}", height=0.66, width=1.85, penwidth=1.2];
  edge [color="{LINE}", fontname="{FONT}", fontsize=12, penwidth=1.1];

  p [label="PendingPayment\\n(chờ thanh toán)", fillcolor="{FILL3}"];
  a [label="Active\\n(đang hiệu lực)", fillcolor="{FILL2}"];
  e [label="Expired\\n(hết hạn)", fillcolor="{FILL1}"];
  c [label="Cancelled\\n(đã hủy)", fillcolor="{FILL1}"];

  p -> a [label=" thanh toán\\n thành công"];
  a -> a [label=" gia hạn /\\n nâng cấp /\\n mua add-on"];
  a -> e [label=" quá hạn"];
  a -> c [label=" yêu cầu hủy"];
  e -> p [label=" đăng ký lại"];
  c -> p [label=" đăng ký\\n lại"];

  {{rank=same; p; a;}}
  {{rank=same; c; e;}}
}}
""", 14, 12.0)

# ── Hình 3.6 (trong báo cáo là Hình 3.13) — Mô hình triển khai ───────────────
render("h3_6_trien_khai", f"""
digraph DEP {{
  rankdir=TB; bgcolor="white"; nodesep=0.22; ranksep=0.50; compound=true;
  node [shape=box, style="filled,rounded", fontname="{FONT}", fontsize=14,
        color="{LINE}", fontcolor="{INK}", penwidth=1.2, height=0.62, width=1.7];
  edge [color="{LINE}", fontname="{FONT}", fontsize=12, penwidth=1.1];

  subgraph cluster_client {{
    label=<<b>Máy khách</b>>; fontname="{FONT}"; fontsize=15; style=rounded; color="{ACC}"; bgcolor="#fffdf6";
    web  [label="Trình duyệt\\n(Razor View + JS)", fillcolor="{FILL2}"];
    mob  [label="Ứng dụng di động\\nReact Native", fillcolor="{FILL2}"];
  }}

  subgraph cluster_server {{
    label=<<b>Máy chủ ứng dụng — Kestrel / IIS</b>>; fontname="{FONT}"; fontsize=15; style=rounded; color="{ACC}"; bgcolor="#fffdf6";
    mvc  [label="ASP.NET Core 8 MVC\\n(Cookie Auth)", fillcolor="{FILL1}"];
    api  [label="REST API\\n(JWT Bearer)", fillcolor="{FILL1}"];
    hub  [label="SignalR Hub\\n(WebSocket)", fillcolor="{FILL1}"];
  }}

  subgraph cluster_data {{
    label=<<b>Dữ liệu và dịch vụ nội bộ</b>>; fontname="{FONT}"; fontsize=15; style=rounded; color="{ACC}"; bgcolor="#fffdf6";
    db   [shape=cylinder, label="SQL Server\\nLuminaTutorsDB", fillcolor="{FILL3}", height=0.8];
    fs   [label="wwwroot/uploads\\n(tệp ≤ 50 MB)", fillcolor="{FILL3}"];
    log  [label="Serilog\\ncuộn theo ngày", fillcolor="{FILL3}"];
    ai   [label="Ollama\\nqwen2.5:7b", fillcolor="{FILL3}"];
    {{rank=same; db; fs;}}
    {{rank=same; log; ai;}}
  }}

  subgraph cluster_ext {{
    label=<<b>Dịch vụ bên thứ ba</b>>; fontname="{FONT}"; fontsize=15; style=rounded; color="{ACC}"; bgcolor="#fffdf6";
    vnp  [label="Cổng thanh toán\\nVNPay", fillcolor="{FILL2}"];
    smtp [label="Máy chủ SMTP", fillcolor="{FILL2}"];
  }}

  web -> mvc [label=" HTTPS"];
  mob -> api [label=" JSON"];
  web -> hub [label=" WSS"];
  mvc -> db; api -> db; hub -> db;
  mvc -> log; api -> ai;
  hub -> vnp [style=invis];
  mvc -> vnp [label="  redirect  "];
  vnp -> mvc [style=dashed, label="  IPN  "];
  vnp -> smtp [style=invis];
  mvc -> smtp;
}}
""", 14, 15.5)

# ── Hình 3.7 (trong báo cáo là Hình 3.6) — Quy trình OLTP ────────────────────
render("h3_7_oltp", f"""
digraph OLTP {{
  rankdir=TB; bgcolor="white"; nodesep=0.30; ranksep=0.70;
  node [shape=box, style="filled,rounded", fontname="{FONT}", fontsize=15,
        color="{LINE}", fontcolor="{INK}", height=0.72, width=1.72, penwidth=1.2];
  edge [color="{LINE}", penwidth=1.3, arrowsize=0.85];

  a [label="1. Chọn\\nsản phẩm", fillcolor="{FILL2}"];
  b [label="2. Giỏ hàng", fillcolor="{FILL2}"];
  c [label="3. Xác nhận\\nđơn", fillcolor="{FILL1}"];
  d [label="4. Tạo\\nđơn hàng", fillcolor="{FILL1}"];
  e [label="5. Thanh toán", fillcolor="{FILL3}"];
  f [label="6. Bàn giao số", fillcolor="{FILL1}"];
  g [label="7. Hoàn tất\\nđơn", fillcolor="{FILL2}"];

  a -> b [constraint=false];
  b -> c [constraint=false];
  c -> d;
  d -> e [constraint=false];
  e -> f [constraint=false];
  f -> g;
  {{ rank=same; a; b; c; }}
  {{ rank=same; d; e; f; }}
}}
""", 15, 15.5)

print(f"{'Tên hình':<26}{'Kích thước px':<16}{'Tỷ lệ':<8}{'Cỡ chữ hiệu dụng':<20}{'Cao (cm)'}")
for r in kiem_tra:
    canh_bao = "  ← nhỏ" if r[3] < 9 else ""
    print(f"{r[0]:<26}{r[1]:<16}{r[2]:<8}{str(r[3]) + ' pt':<20}{r[4]}{canh_bao}")
