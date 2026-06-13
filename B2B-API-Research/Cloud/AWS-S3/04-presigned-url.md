# AWS S3 — Presigned URL

## Presigned URL là gì?

URL tạm thời có chứa thông tin xác thực, cho phép bất kỳ ai có URL đó thực hiện một thao tác cụ thể (upload hoặc download) trong một khoảng thời gian nhất định — **không cần AWS credentials**.

```
https://{bucket}.s3.{region}.amazonaws.com/{key}
  ?X-Amz-Algorithm=AWS4-HMAC-SHA256
  &X-Amz-Credential=AKIAIOSFODNN7...
  &X-Amz-Date=20240612T143000Z
  &X-Amz-Expires=3600
  &X-Amz-SignedHeaders=host
  &X-Amz-Signature=abc123...
```

---

## Query Parameters của Presigned URL

| Parameter | Mô tả |
|-----------|-------|
| `X-Amz-Algorithm` | Thuật toán ký: luôn `AWS4-HMAC-SHA256` |
| `X-Amz-Credential` | `{AccessKey}/{Date}/{Region}/s3/aws4_request` |
| `X-Amz-Date` | Thời điểm tạo URL: `yyyyMMddTHHmmssZ` |
| `X-Amz-Expires` | Thời gian hiệu lực tính bằng **giây** (max 7 ngày = 604800s) |
| `X-Amz-SignedHeaders` | Các header tham gia ký: thường `host` |
| `X-Amz-Signature` | HMAC-SHA256 signature |

---

## Use case trong Lumina Tutors

| Tình huống | Loại | Thời gian |
|-----------|------|-----------|
| Upload avatar học sinh | PUT Presigned URL | 5 phút |
| Xem tài liệu khóa học (private) | GET Presigned URL | 1 giờ |
| Download báo cáo học phí | GET Presigned URL | 15 phút |
| Upload bài kiểm tra từ client | PUT Presigned URL | 10 phút |

---

## GET Presigned URL (Download)

```csharp
public string GenerateDownloadUrl(string key, int expirySeconds = 3600)
{
    var request = new GetPreSignedUrlRequest
    {
        BucketName = _bucketName,
        Key        = key,
        Verb       = HttpVerb.GET,
        Expires    = DateTime.UtcNow.AddSeconds(expirySeconds)
    };

    return _s3Client.GetPreSignedURL(request);
}
```

**Ví dụ URL tạo ra:**
```
https://lumina-tutors.s3.ap-southeast-1.amazonaws.com/reports/2024/06/report-001.pdf
  ?X-Amz-Algorithm=AWS4-HMAC-SHA256
  &X-Amz-Credential=AKIAIOSFODNN7%2F20240612%2Fap-southeast-1%2Fs3%2Faws4_request
  &X-Amz-Date=20240612T143000Z
  &X-Amz-Expires=3600
  &X-Amz-SignedHeaders=host
  &X-Amz-Signature=abc123...
```

---

## PUT Presigned URL (Upload từ client)

Luồng: **Client lấy URL từ server → Client upload thẳng lên S3** (không qua server).

```
[Browser/Mobile] ──GET /api/upload-url──► [Lumina Server]
                                                │
                                    Tạo PUT Presigned URL
                                                │
                  ◄──── { uploadUrl, fileKey } ──┘
                  │
                  └──PUT {uploadUrl}──► [S3 trực tiếp]
```

### Server tạo Presigned URL

```csharp
[HttpPost("api/files/upload-url")]
public async Task<IActionResult> GetUploadUrl([FromBody] UploadUrlRequest req)
{
    // Validate file type
    var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf" };
    if (!allowedTypes.Contains(req.ContentType))
        return BadRequest("File type not allowed");

    var ext    = req.ContentType == "application/pdf" ? ".pdf" : ".jpg";
    var key    = $"uploads/{req.Folder}/{Guid.NewGuid()}{ext}";

    var urlRequest = new GetPreSignedUrlRequest
    {
        BucketName  = _bucketName,
        Key         = key,
        Verb        = HttpVerb.PUT,
        Expires     = DateTime.UtcNow.AddMinutes(5),
        ContentType = req.ContentType
    };

    var uploadUrl = _s3Client.GetPreSignedURL(urlRequest);

    return Ok(new { uploadUrl, fileKey = key });
}
```

### Client sử dụng Presigned URL

```typescript
// Frontend (TypeScript)
async function uploadFile(file: File, folder: string) {
    // Bước 1: Lấy presigned URL từ server
    const { uploadUrl, fileKey } = await fetch('/api/files/upload-url', {
        method: 'POST',
        body: JSON.stringify({ contentType: file.type, folder }),
        headers: { 'Content-Type': 'application/json' }
    }).then(r => r.json());

    // Bước 2: Upload thẳng lên S3
    await fetch(uploadUrl, {
        method: 'PUT',
        body: file,
        headers: { 'Content-Type': file.type }
    });

    // Bước 3: Lưu fileKey vào server
    await fetch('/api/files/confirm', {
        method: 'POST',
        body: JSON.stringify({ fileKey }),
        headers: { 'Content-Type': 'application/json' }
    });

    return fileKey;
}
```

---

## Lưu ý

- Presigned URL mang credentials của IAM user/role tạo ra nó — nếu key bị xóa, URL cũng mất hiệu lực
- Không share Presigned URL rộng rãi, đặt thời gian ngắn nhất có thể
- Với file public (avatar), có thể dùng CloudFront URL thay vì presigned URL để cache tốt hơn
