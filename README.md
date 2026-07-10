# 🎓 Lumina Tutors — Hệ Thống Quản Lý Giáo Dục

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-MVC-blueviolet?style=flat-square" />
  <img src="https://img.shields.io/badge/React_Native-Expo_SDK_56-000020?style=flat-square&logo=expo" />
  <img src="https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver" />
  <img src="https://img.shields.io/badge/Ollama-AI_Tutor-black?style=flat-square" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" />
</p>

> Nền tảng quản lý giáo dục đa trường (multi-tenant) dành cho các trường tư thục Việt Nam — bao gồm quản lý học vụ, điểm danh QR, sổ điểm TT22, tài chính, nhân sự, kỷ luật, giao tiếp, và **Gia Sư AI** chạy hoàn toàn offline bằng Ollama.

---

## ✨ Tính năng nổi bật

| Module | Mô tả |
|--------|-------|
| 🏫 **Học vụ** | Quản lý năm học, học kỳ, lớp, xếp lớp, phân công giáo viên |
| ✅ **Điểm danh** | QR Code, thủ công, báo cáo vắng mặt, thông báo phụ huynh |
| 📊 **Sổ điểm TT22** | Nhập điểm ĐTX/ĐGK/ĐCK, tính ĐTBm tự động theo Thông tư 22/2021 |
| 💰 **Tài chính** | Cấu hình học phí, xuất hóa đơn hàng loạt, theo dõi công nợ |
| 👥 **Nhân sự** | Hợp đồng giáo viên, chấm công, tính lương, đánh giá |
| ⚖️ **Kỷ luật** | Ghi nhận vi phạm, báo cáo hàng ngày, thống kê theo lớp |
| 📢 **Giao tiếp** | Thông báo đẩy, nhắn tin nội bộ, bảng tin trường |
| 🧠 **Gia Sư AI** | Chatbot học tập chạy offline với Ollama (qwen2.5:7b), kiểm duyệt nội dung bởi Admin |
| 📱 **Mobile App** | React Native (Expo) cho Giáo viên, Học sinh, Phụ huynh, Giám thị |
| 🧪 **Phòng Lab 3D** | Thí nghiệm ảo tương tác |
| 🎥 **Lớp học Online** | Phòng học realtime với WebRTC + SignalR |

---

## 🏗️ Kiến trúc

```
Clean Architecture (4 tầng)
┌─────────────────────────────────────┐
│  Web  (ASP.NET Core 8 MVC + API)    │  ← Controllers, Views, Hubs
├─────────────────────────────────────┤
│  Infrastructure  (.NET 8)           │  ← EF Core, Repositories, Ollama
├─────────────────────────────────────┤
│  Application  (.NET 8)              │  ← Services, DTOs, Validators
├─────────────────────────────────────┤
│  Domain  (.NET 8)                   │  ← Entities, Interfaces, Enums
└─────────────────────────────────────┘

Mobile: React Native (Expo SDK 56)
API:    REST + JWT Bearer (/api/...)
RT:     SignalR WebSocket
```

---

## 🚀 Cài đặt & Chạy

### Yêu cầu
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server 2019+](https://www.microsoft.com/sql-server) hoặc LocalDB
- [Node.js 18+](https://nodejs.org) (cho mobile)
- [Ollama](https://ollama.com) (cho Gia Sư AI — tùy chọn)

### 1. Clone & cấu hình
```bash
git clone https://github.com/<your-username>/lumina-tutors.git
cd lumina-tutors
```

Chỉnh connection string trong `src/LuminaTutors.Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "LuminaTutorsDb": "Server=.\\SQLEXPRESS;Database=LuminaTutorsDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 2. Migrate database & seed dữ liệu
```bash
dotnet ef database update --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web
```
> Dữ liệu mẫu (seed) được tự động tạo khi chạy ở môi trường Development.

### 3. Chạy web app
```bash
dotnet run --project src/LuminaTutors.Web
```
Mở trình duyệt: `https://localhost:60480`

### 4. Cài Gia Sư AI (Ollama)
```bash
# Cài Ollama từ https://ollama.com/download
ollama pull qwen2.5:7b
```
Thêm vào `appsettings.json`:
```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "qwen2.5:7b"
}
```

### 5. Chạy mobile app
```bash
cd lumina-mobile
npm install
# Đổi IP trong src/api/client.ts thành IP máy tính
npx expo start
```
Quét QR bằng **Expo Go** trên điện thoại.

---

## 👤 Tài khoản mặc định (Seed)

| Role | Email | Mật khẩu |
|------|-------|----------|
| Admin | admin@lumina.edu.vn | Admin@123 |
| Giáo viên | teacher@lumina.edu.vn | Teacher@123 |
| Học sinh | student@lumina.edu.vn | Student@123 |
| Phụ huynh | parent@lumina.edu.vn | Parent@123 |

---

## 📱 REST API (Mobile)

Base URL: `http://<server>/api`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/auth/login` | Đăng nhập → JWT token |
| GET | `/mobile/student/grades` | Điểm học sinh |
| GET | `/mobile/student/attendance` | Điểm danh |
| GET | `/mobile/teacher/classes` | Lớp giáo viên |
| GET | `/mobile/teacher/gradebook/{id}` | Sổ điểm |
| GET | `/mobile/parent/child-grades` | Điểm con |
| GET | `/mobile/supervisor/discipline` | Vi phạm |
| GET | `/mobile/notifications` | Thông báo |

---

## 🗂️ Cấu trúc thư mục

```
lumina-tutors/
├── src/
│   ├── LuminaTutors.Domain/          # Entities, Enums, Interfaces
│   ├── LuminaTutors.Application/     # Services, DTOs, Validators
│   ├── LuminaTutors.Infrastructure/  # EF Core, Repositories, Ollama
│   └── LuminaTutors.Web/             # MVC Controllers, Views, API
├── tests/
│   ├── LuminaTutors.UnitTests/
│   └── LuminaTutors.IntegrationTests/
├── lumina-mobile/                    # React Native (Expo)
│   └── src/
│       ├── api/         # Axios client
│       ├── context/     # AuthContext (JWT)
│       ├── screens/     # Student, Teacher, Parent, Supervisor
│       └── theme/       # Colors, typography
└── README.md
```

---

## 🧪 Chạy Tests

```bash
# Tất cả tests
dotnet test

# Unit tests
dotnet test tests/LuminaTutors.UnitTests

# Integration tests
dotnet test tests/LuminaTutors.IntegrationTests
```

---

## 🛠️ Tech Stack

**Backend**
- ASP.NET Core 8.0 MVC + Web API
- Entity Framework Core 8 (SQL Server)
- SignalR (realtime)
- Serilog (logging)
- AutoMapper + FluentValidation
- xUnit + Moq + SpecFlow (BDD)

**AI**
- Ollama (local LLM runtime)
- Model: qwen2.5:7b (hỗ trợ tiếng Việt)

**Mobile**
- React Native + Expo SDK 56
- React Navigation v7
- Axios + Expo SecureStore (JWT)

---

## 📄 License

MIT © 2026 Lumina Tutors
