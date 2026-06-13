# AWS S3 — Upload File (PutObject)

## REST API trực tiếp

```
PUT https://{bucket}.s3.{region}.amazonaws.com/{key}
```

### Request Headers

| Header | Bắt buộc | Mô tả | Ví dụ |
|--------|----------|-------|-------|
| `Authorization` | ✅ | SigV4 signature | `AWS4-HMAC-SHA256 Credential=AKIA.../...` |
| `x-amz-date` | ✅ | Timestamp UTC: `yyyyMMddTHHmmssZ` | `20240612T143000Z` |
| `x-amz-content-sha256` | ✅ | SHA256 của body. Nếu không tính: `UNSIGNED-PAYLOAD` | `9f86d081...` |
| `Content-Type` | ✅ | MIME type của file | `image/jpeg`, `application/pdf` |
| `Content-Length` | ✅ | Kích thước body tính bằng byte | `204800` |
| `x-amz-acl` | ❌ | Quyền truy cập: `private` / `public-read` | `public-read` |
| `x-amz-storage-class` | ❌ | Loại lưu trữ | `STANDARD` (default), `STANDARD_IA`, `GLACIER` |
| `x-amz-server-side-encryption` | ❌ | Mã hóa phía server | `AES256` |
| `x-amz-meta-{name}` | ❌ | Metadata tùy chỉnh | `x-amz-meta-uploaded-by: teacher-01` |

### Request Body

Raw bytes của file.

### Response Headers

| Header | Mô tả |
|--------|-------|
| `ETag` | MD5 hash của object (dùng để verify) |
| `x-amz-version-id` | Version ID nếu bucket bật versioning |
| `x-amz-server-side-encryption` | Thuật toán mã hóa đã dùng |

### Response Status

| Code | Ý nghĩa |
|------|---------|
| `200 OK` | Upload thành công |
| `403 Forbidden` | Sai credentials hoặc không có quyền |
| `404 Not Found` | Bucket không tồn tại |
| `413 Request Entity Too Large` | File vượt giới hạn (max 5GB cho single PUT) |

---

## SDK .NET — Cách thông dụng nhất

### Upload từ Stream (IFormFile)

```csharp
public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client   = s3Client;
        _bucketName = config["AWS:BucketName"];
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        // Tạo key theo cấu trúc thư mục
        var ext = Path.GetExtension(file.FileName);
        var key = $"{folder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{ext}";

        var request = new PutObjectRequest
        {
            BucketName  = _bucketName,
            Key         = key,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            // Không set ACL nếu bucket bật "Block Public Access" + dùng Presigned URL
            Metadata =
            {
                ["uploaded-by"] = "lumina-system",
                ["original-name"] = file.FileName
            }
        };

        var response = await _s3Client.PutObjectAsync(request);

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception($"S3 upload failed: {response.HttpStatusCode}");

        return key; // Lưu key này vào DB, không lưu full URL
    }
}
```

### Upload từ byte[]

```csharp
public async Task<string> UploadBytesAsync(byte[] data, string key, string contentType)
{
    using var stream = new MemoryStream(data);

    var request = new PutObjectRequest
    {
        BucketName  = _bucketName,
        Key         = key,
        InputStream = stream,
        ContentType = contentType
    };

    await _s3Client.PutObjectAsync(request);
    return key;
}
```

---

## Multipart Upload (File > 100MB)

Dùng khi upload file lớn — chia nhỏ thành nhiều phần upload song song:

```csharp
// Bước 1: Khởi tạo
var initResp = await _s3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
{
    BucketName = _bucketName,
    Key        = key,
    ContentType = "video/mp4"
});
var uploadId = initResp.UploadId;

// Bước 2: Upload từng phần (min 5MB mỗi phần)
var partETags = new List<PartETag>();
for (int i = 0; i < totalParts; i++)
{
    var partResp = await _s3Client.UploadPartAsync(new UploadPartRequest
    {
        BucketName   = _bucketName,
        Key          = key,
        UploadId     = uploadId,
        PartNumber   = i + 1,
        InputStream  = partStream,
        PartSize     = partSize
    });
    partETags.Add(new PartETag(i + 1, partResp.ETag));
}

// Bước 3: Complete
await _s3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
{
    BucketName = _bucketName,
    Key        = key,
    UploadId   = uploadId,
    PartETags  = partETags
});
```

---

## Cấu trúc Key khuyến nghị cho Lumina Tutors

```
students/{schoolId}/{studentId}/avatar.jpg
documents/{schoolId}/courses/{courseId}/{filename}
reports/{schoolId}/{year}/{month}/report-{timestamp}.pdf
attendance/qr/{schoolId}/{classId}/{date}.png
```

**Lưu key vào DB, không lưu full URL** — URL sẽ thay đổi khi đổi bucket/CDN.
