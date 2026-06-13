# AWS S3 — Xóa File & Bucket Policy

## Xóa một file (DeleteObject)

### REST API

```
DELETE https://{bucket}.s3.{region}.amazonaws.com/{key}
Authorization: AWS4-HMAC-SHA256 ...
x-amz-date: 20240612T143000Z
```

**Response:** `204 No Content` nếu thành công (kể cả khi key không tồn tại).

### SDK .NET

```csharp
public async Task DeleteAsync(string key)
{
    await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
    {
        BucketName = _bucketName,
        Key        = key
    });
}
```

---

## Xóa nhiều file cùng lúc (DeleteObjects)

```csharp
public async Task DeleteManyAsync(IEnumerable<string> keys)
{
    var request = new DeleteObjectsRequest
    {
        BucketName = _bucketName,
        Objects    = keys.Select(k => new KeyVersion { Key = k }).ToList()
    };

    var response = await _s3Client.DeleteObjectsAsync(request);

    // Kiểm tra lỗi
    if (response.DeleteErrors.Any())
    {
        var errors = string.Join(", ", response.DeleteErrors.Select(e => $"{e.Key}: {e.Message}"));
        throw new Exception($"Some files failed to delete: {errors}");
    }
}
```

---

## Bucket Policy — Cấu hình quyền truy cập

Bucket Policy là JSON document định nghĩa ai được làm gì với bucket.

### Policy cho Lumina Tutors (khuyến nghị)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowLuminaAppAccess",
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::123456789:user/lumina-app-user"
      },
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectMetadata"
      ],
      "Resource": "arn:aws:s3:::lumina-tutors/*"
    },
    {
      "Sid": "DenyPublicAccess",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::lumina-tutors/private/*"
    }
  ]
}
```

### CORS Policy (cần thiết cho upload từ browser)

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["PUT", "GET"],
    "AllowedOrigins": ["https://lumina.vn", "http://localhost:3000"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```

---

## IAM Policy tối thiểu cho Lumina App

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::lumina-tutors",
        "arn:aws:s3:::lumina-tutors/*"
      ]
    }
  ]
}
```

---

## Bảng Response Code S3

| HTTP Code | Ý nghĩa |
|-----------|---------|
| `200 OK` | Thành công (GET) |
| `204 No Content` | Thành công (DELETE, PUT không body) |
| `400 Bad Request` | Request sai format |
| `403 Forbidden` | Không có quyền hoặc sai credentials |
| `404 Not Found` | Bucket hoặc object không tồn tại |
| `409 Conflict` | Bucket đã tồn tại |
| `500 Internal Server Error` | Lỗi phía AWS |
| `503 Service Unavailable` | AWS đang có sự cố hoặc throttle |
