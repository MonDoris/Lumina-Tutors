using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Services;

/// <summary>
/// Đổi <c>Email:RefreshToken</c> (lấy 1 lần qua đồng ý OAuth) lấy access token ngắn hạn
/// để xác thực SMTP Gmail bằng cơ chế XOAUTH2. Access token được cache tới gần lúc hết hạn.
///
/// Cần <c>Email:ClientId</c>, <c>Email:ClientSecret</c>, <c>Email:RefreshToken</c>
/// (đặt bằng user-secrets, KHÔNG commit).
/// </summary>
public interface IGmailTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken ct = default);
}

public sealed class GmailOAuthTokenProvider : IGmailTokenProvider
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration     _config;
    private readonly ILogger<GmailOAuthTokenProvider> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string?  _cachedToken;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    public GmailOAuthTokenProvider(
        IHttpClientFactory httpFactory, IConfiguration config, ILogger<GmailOAuthTokenProvider> logger)
    {
        _httpFactory = httpFactory;
        _config      = config;
        _logger      = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Còn hạn (chừa 60s an toàn) → dùng lại.
        if (_cachedToken is not null && DateTime.UtcNow < _expiresAtUtc.AddSeconds(-60))
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTime.UtcNow < _expiresAtUtc.AddSeconds(-60))
                return _cachedToken;

            var clientId     = _config["Email:ClientId"];
            var clientSecret = _config["Email:ClientSecret"];
            var refreshToken = _config["Email:RefreshToken"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("Thiếu ClientId/ClientSecret/RefreshToken cho Gmail OAuth.");
                return null;
            }

            var http = _httpFactory.CreateClient("GmailOAuth");
            using var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"]    = "refresh_token"
            });

            using var resp = await http.PostAsync(TokenEndpoint, body, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Đổi refresh token thất bại ({Status}): {Body}", (int)resp.StatusCode, json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var expiresIn   = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Phản hồi token không có access_token: {Body}", json);
                return null;
            }

            _cachedToken  = accessToken;
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);
            _logger.LogInformation("Lấy được Gmail access token, hết hạn sau {Sec}s", expiresIn);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy Gmail access token");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
