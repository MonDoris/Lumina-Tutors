# Lumina · Đài quan sát — Dashboard Sysadmin "Đồng Vọng"

Dashboard quản trị hệ thống lấy cảm hứng từ trống đồng Đông Sơn: mạng lưới dịch vụ
xếp theo vòng đồng tâm, node đập nhịp như tim, chuyển trang bằng hiệu ứng "zoom vào
node" không tải lại trang.

Stack: **Vite + React 18 + TypeScript + Tailwind CSS 3 + Framer Motion 11**.

## Chạy dự án

```powershell
cd lumina-sysadmin
npm install        # lần đầu tiên
npm run dev        # mở http://localhost:5173
```

Build production:

```powershell
npm run build      # kiểm tra TypeScript + xuất ra dist/
npm run preview    # xem thử bản build
```

## Cách điều hướng

- **Mặt trống (Overview)** — mỗi node là một dịch vụ. Màu = trạng thái:
  ngọc bích (khỏe), vàng đồng (suy giảm), đỏ son (nguy kịch). Nhịp đập càng
  nhanh nghĩa là tình trạng càng xấu.
- **Click vào node** — camera "lặn" vào node, mở tiểu vũ trụ chức năng của nó.
- **Cây tri thức (mép trái)** — điều hướng song song; hover để xem lá con.
- **"↑ Trồi lên mặt trống"** — quay về Overview bằng hiệu ứng zoom ngược.
- **Hồ cảnh báo** — rê chuột lên thẻ cảnh báo để thấy gợn sóng mặt hồ.

## Cấu trúc mã nguồn

```
src/
├─ main.tsx                  điểm vào React
├─ SysadminUniverse.tsx      bố cục gốc: nền + trống + chrome
├─ DiveStage.tsx             AnimatePresence — chuyển cảnh dive/surface
├─ dive-context.tsx          state điều hướng + tọa độ tâm zoom
├─ data.ts                   NODES / FLOWS / MODULES / BRANCHES (demo data)
├─ features/topology/
│  └─ DongSonTopology.tsx    trống đồng SVG: mặt trời 14 tia, vòng, chim Lạc
└─ components/
   ├─ AmbientLayer.tsx       sương nền + hạt trôi
   ├─ CitadelHeader.tsx      tiêu đề serif + breadcrumb + đồng hồ
   ├─ TreeOfKnowledgeNav.tsx cây tri thức (nav ngữ nghĩa, a11y)
   ├─ TelemetryRail.tsx      sinh hiệu hệ thống (mô phỏng jitter)
   ├─ ModuleUniverse.tsx     tiểu vũ trụ khi dive vào node
   └─ AlertLotusCard.tsx     thẻ cảnh báo với hiệu ứng gợn sóng
```

## Nối dữ liệu thật

Toàn bộ dữ liệu demo nằm ở `src/data.ts`. Để nối vào backend ASP.NET:

1. Tạo endpoint JSON phía `LuminaTutors.Web` (ví dụ `GET /api/sysadmin/topology`,
   `GET /api/sysadmin/vitals`) trả về đúng shape của `DrumNode[]` và `ModuleDef`.
2. Thay các hằng `NODES/FLOWS/MODULES` bằng fetch + polling (hoặc SignalR).
3. Deploy: `npm run build` rồi copy `dist/` vào `wwwroot/sysadmin` của
   LuminaTutors.Web, thêm route `/sysadmin` gác bằng policy role SYSADMIN.

## Ghi chú

- Fonts (Cormorant, Be Vietnam Pro, IBM Plex Mono) tải từ Google Fonts,
  đều có subset tiếng Việt.
- Đã hỗ trợ `prefers-reduced-motion`: người dùng bật giảm chuyển động sẽ
  thấy chuyển cảnh fade thay vì zoom.
