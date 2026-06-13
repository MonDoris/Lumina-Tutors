# AWS S3 — Tổng quan

## S3 là gì?

Amazon Simple Storage Service (S3) là dịch vụ lưu trữ object (file) trên cloud của AWS. Với Lumina Tutors, S3 dùng để lưu: avatar học sinh/giáo viên, tài liệu học tập, file xuất báo cáo, ảnh QR attendance.

---

## Khái niệm cơ bản

| Khái niệm | Ý nghĩa |
|-----------|---------|
| **Bucket** | Container chứa file, tương tự "ổ đĩa". Tên phải unique toàn cầu. |
| **Object** | Một file được lưu trong S3. Mỗi object có: Key (đường dẫn), Body (nội dung), Metadata. |
| **Key** | Tên/đường dẫn của object trong bucket. VD: `students/2024/avatar-001.jpg` |
| **Region** | Vùng địa lý lưu dữ liệu. VN nên dùng `ap-southeast-1` (Singapore) |
| **ACL** | Access Control List — quyền truy cập public/private |

---

## Cấu trúc URL

```
https://{bucket}.s3.{region}.amazonaws.com/{key}
```

Ví dụ:
```
https://lumina-tutors.s3.ap-southeast-1.amazonaws.com/students/2024/avatar-001.jpg
```

---

## Authentication

AWS S3 dùng **Signature Version 4 (SigV4)** — mọi request phải có chữ ký.

### Headers bắt buộc cho mọi request S3

| Header | Mô tả | Ví dụ |
|--------|-------|-------|
| `Authorization` | SigV4 signature đầy đủ | `AWS4-HMAC-SHA256 Credential=...` |
| `x-amz-date` | Timestamp ISO 8601 | `20240612T143000Z` |
| `x-amz-content-sha256` | SHA256 hash của request body | `e3b0c44298...` (empty = `UNSIGNED-PAYLOAD`) |
| `Host` | Tên bucket + endpoint | `lumina-tutors.s3.ap-southeast-1.amazonaws.com` |

> **Thực tế:** Không cần tạo SigV4 thủ công — dùng **AWS SDK for .NET** sẽ tự xử lý.

---

## Cài đặt AWS SDK (.NET)

```bash
dotnet add package AWSSDK.S3
```

```csharp
// appsettings.json
{
  "AWS": {
    "Region": "ap-southeast-1",
    "BucketName": "lumina-tutors",
    "AccessKey": "AKIA...",
    "SecretKey": "...",
    "CloudFrontDomain": "https://cdn.lumina.vn"
  }
}
```

```csharp
// Program.cs
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<IS3Service, S3Service>();
```

---

## Danh sách tài liệu trong thư mục này

| File | Nội dung |
|------|----------|
| `02-upload-object.md` | Upload file lên S3 (PutObject) |
| `03-get-download-object.md` | Download / lấy URL file |
| `04-presigned-url.md` | Presigned URL — cho phép upload/download mà không cần credentials |
| `05-delete-object.md` | Xóa file |
| `06-bucket-policy.md` | Cấu hình quyền truy cập bucket |
