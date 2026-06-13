# AWS S3 — Download / Lấy File (GetObject)

## REST API trực tiếp

```
GET https://{bucket}.s3.{region}.amazonaws.com/{key}
```

### Request Headers

| Header | Bắt buộc | Mô tả |
|--------|----------|-------|
| `Authorization` | ✅ | SigV4 signature |
| `x-amz-date` | ✅ | Timestamp UTC |
| `Range` | ❌ | Download một phần: `bytes=0-1023` |
| `If-Modified-Since` | ❌ | Chỉ trả nếu thay đổi sau thời điểm này |
| `If-None-Match` | ❌ | Conditional get theo ETag |

### Response Headers

| Header | Mô tả |
|--------|-------|
| `Content-Type` | MIME type của file |
| `Content-Length` | Kích thước file (bytes) |
| `ETag` | MD5 hash của file |
| `Last-Modified` | Thời gian sửa đổi cuối |
| `x-amz-meta-*` | Metadata tùy chỉnh |

### Response Body

Raw bytes của file.

---

## SDK .NET

### Download về MemoryStream

```csharp
public async Task<Stream> DownloadAsync(string key)
{
    var request = new GetObjectRequest
    {
        BucketName = _bucketName,
        Key        = key
    };

    var response = await _s3Client.GetObjectAsync(request);

    var ms = new MemoryStream();
    await response.ResponseStream.CopyToAsync(ms);
    ms.Position = 0;
    return ms;
}
```

### Download và gửi thẳng về client (ASP.NET Core)

```csharp
[HttpGet("files/{*key}")]
public async Task<IActionResult> DownloadFile(string key)
{
    var response = await _s3Client.GetObjectAsync(new GetObjectRequest
    {
        BucketName = _bucketName,
        Key        = key
    });

    return File(
        response.ResponseStream,
        response.Headers.ContentType,
        Path.GetFileName(key)  // Tên file khi download
    );
}
```

---

## Lấy URL public của file

### Nếu bucket public (không khuyến nghị cho production)

```csharp
public string GetPublicUrl(string key)
{
    return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
}
```

### Nếu dùng CloudFront CDN (khuyến nghị)

```csharp
public string GetCdnUrl(string key)
{
    return $"{_cloudFrontDomain}/{key}";
    // Ví dụ: https://cdn.lumina.vn/students/avatar.jpg
}
```

---

## Kiểm tra file tồn tại (GetObjectMetadata)

```csharp
public async Task<bool> ExistsAsync(string key)
{
    try
    {
        await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key        = key
        });
        return true;
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return false;
    }
}
```

---

## List các file trong một "thư mục"

```csharp
public async Task<List<string>> ListFilesAsync(string prefix)
{
    var request = new ListObjectsV2Request
    {
        BucketName = _bucketName,
        Prefix     = prefix,  // VD: "students/school-1/"
        MaxKeys    = 100
    };

    var keys = new List<string>();
    ListObjectsV2Response response;

    do
    {
        response = await _s3Client.ListObjectsV2Async(request);
        keys.AddRange(response.S3Objects.Select(o => o.Key));
        request.ContinuationToken = response.NextContinuationToken;
    } while (response.IsTruncated);

    return keys;
}
```
