using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LuminaTutors.Web.Services.VnPay;

/// <summary>
/// Thư viện ký &amp; duyệt chữ ký VNPay 2.1.0 (HMAC-SHA512).
/// Cả query string lẫn dữ liệu ký đều được URL-encode + sắp xếp theo key (alphabet)
/// — đúng chuẩn để VNPay xác thực được. Dùng cùng một bộ encode cho cả 2 chiều.
/// </summary>
public sealed class VnPayLibrary
{
    private readonly SortedList<string, string> _request  = new(StringComparer.Ordinal);
    private readonly SortedList<string, string> _response = new(StringComparer.Ordinal);

    public void AddRequestData(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value)) _request[key] = value!;
    }

    public void AddResponseData(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value)) _response[key] = value!;
    }

    public string? GetResponseData(string key) => _response.TryGetValue(key, out var v) ? v : null;

    /// <summary>Dựng URL thanh toán hoàn chỉnh kèm vnp_SecureHash.</summary>
    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var query = BuildSignData(_request, includeAll: true);
        var secureHash = HmacSha512(hashSecret, query);
        return $"{baseUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    /// <summary>Xác thực chữ ký của callback (IPN / ReturnURL).</summary>
    public bool ValidateSignature(string? inputHash, string hashSecret)
    {
        if (string.IsNullOrEmpty(inputHash)) return false;
        var raw = BuildSignData(_response, includeAll: false);
        var computed = HmacSha512(hashSecret, raw);
        return computed.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSignData(SortedList<string, string> data, bool includeAll)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (!includeAll && (key is "vnp_SecureHash" or "vnp_SecureHashType")) continue;
            if (string.IsNullOrEmpty(value)) continue;
            sb.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value)).Append('&');
        }
        if (sb.Length > 0) sb.Length--; // bỏ '&' cuối
        return sb.ToString();
    }

    private static string HmacSha512(string key, string input)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
