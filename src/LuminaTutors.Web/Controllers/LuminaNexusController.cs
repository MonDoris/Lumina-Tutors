using System.Security.Claims;
using System.Text.Json;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// "Lumina Holographic Nexus" — phòng học 3D real-time + streaming.
/// View là một trang toàn màn hình (Layout = null) host &lt;canvas&gt; Three.js,
/// render hình chiếu hologram, overlay Glassmorphism tối, và kết nối SignalR
/// tới /hubs/lumina-rtc (SFU thuần C#).
///
/// Giáo viên chọn MÔN HỌC + THÍ NGHIỆM ngay trong phòng; mặc định mở đúng môn
/// giáo viên đang dạy (tự dò qua hồ sơ tài khoản).
/// </summary>
[Authorize(Policy = "LabAccess")]
public sealed class LuminaNexusController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IMemoryCache    _cache;
    private readonly IConfiguration  _config;

    public LuminaNexusController(IAccountService accountService, IMemoryCache cache, IConfiguration config)
    {
        _accountService = accountService;
        _cache          = cache;
        _config         = config;
    }

    /// <summary>
    /// ICE servers cho client (cùng bộ với SFU). Trả JSON dạng mảng
    /// [{ urls, username?, credential? }] để nhúng vào NEXUS_CONFIG.iceServers.
    /// Thêm TURN trong cấu hình "Webrtc:IceServers" để điện thoại ở mạng khác /
    /// sau NAT đối xứng kết nối được. Mặc định chỉ STUN Google.
    /// </summary>
    private string BuildClientIceServersJson()
    {
        var list = new List<Dictionary<string, string>>();
        foreach (var node in _config.GetSection("Webrtc:IceServers").GetChildren())
        {
            var urls = node["urls"];
            if (string.IsNullOrWhiteSpace(urls)) continue;
            var entry = new Dictionary<string, string> { ["urls"] = urls };
            var username   = node["username"];
            var credential = node["credential"];
            if (!string.IsNullOrWhiteSpace(username))   entry["username"]   = username;
            if (!string.IsNullOrWhiteSpace(credential)) entry["credential"] = credential;
            list.Add(entry);
        }
        if (list.Count == 0)
            list.Add(new Dictionary<string, string> { ["urls"] = "stun:stun.l.google.com:19302" });
        return JsonSerializer.Serialize(list);
    }

    // Bộ thẻ môn học dùng trong phòng Lab 3D (đồng bộ với VirtualLab)
    private static readonly string[] ValidTags = ["chemistry", "physics", "biology", "math"];

    private static readonly (string[] Keywords, string Tag)[] SubjectTagMap =
    [
        (["hóa", "hoá", "chemistry"], "chemistry"),
        (["vật lý", "vat ly", "physics"], "physics"),
        (["sinh", "biology"], "biology"),
        (["toán", "toan", "math"], "math"),
    ];

    private static string? GuessSubjectTag(string? subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName)) return null;
        var lower = subjectName.ToLowerInvariant();
        foreach (var (keywords, tag) in SubjectTagMap)
            if (keywords.Any(k => lower.Contains(k)))
                return tag;
        return null;
    }

    // Mã phòng = chuỗi 6 CHỮ SỐ (100000–999999) để học sinh gõ nhanh bằng bàn
    // phím số trên điện thoại; luôn đủ 6 số, không có số 0 đứng đầu gây nhầm.
    private static string GenerateRoomCode() => Random.Shared.Next(100000, 1000000).ToString();

    // ─── GET /LuminaNexus/Create — giáo viên tạo phòng mới (mã 6 ký tự) ───────
    [HttpGet]
    public IActionResult Create(string? subject = null)
    {
        if (!(User.IsInRole("TEACHER") || User.IsInRole("ADMIN")))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Index), new { room = GenerateRoomCode(), subject });
    }

    // ─── GET /LuminaNexus?room=ABC123&subject=biology&scene=cell ──────────────
    public async Task<IActionResult> Index(string room = "nexus-demo", string? subject = null, string? scene = null)
    {
        var isTeacher = User.IsInRole("TEACHER") || User.IsInRole("ADMIN");

        // Chuẩn hoá mã phòng HOA để teacher/student luôn khớp group dù gõ thường.
        var roomId = string.IsNullOrWhiteSpace(room) ? "NEXUS-DEMO" : room.Trim().ToUpperInvariant();
        ViewBag.RoomId      = roomId;
        ViewBag.IsTeacher   = isTeacher;
        ViewBag.DisplayName = User.FindFirstValue(ClaimTypes.Name) ?? "Học viên";

        // Môn học mặc định: ưu tiên tham số → môn giáo viên đang dạy → null (UI fallback)
        var subjectTag = ValidTags.Contains(subject) ? subject : null;
        if (subjectTag is null && isTeacher)
        {
            var schoolId = int.Parse(User.FindFirstValue("SchoolId") ?? "0");
            var userId   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var profile  = await _accountService.GetAccountByIdAsync(schoolId, userId);
            if (profile.IsSuccess)
                subjectTag = GuessSubjectTag(profile.Data!.PrimarySubjectName ?? profile.Data.SpecializationSubject);
        }

        ViewBag.DefaultSubjectTag = subjectTag;
        ViewBag.InitialScene      = string.IsNullOrWhiteSpace(scene) ? null : scene.Trim();
        ViewBag.IceServersJson    = BuildClientIceServersJson();

        return View();
    }

    // ─── GET /LuminaNexus/MobileEntry?room=123456&code=GUID ───────────────────
    // Cầu nối WebView cho app: app đã xác thực JWT → tạo bridge code 90s
    // (POST /api/mobile/webview-token) → mở WebView ở URL này. Ở đây đổi bridge
    // code thành cookie rồi vào thẳng phòng 3D (SignalR realtime chạy bằng cookie).
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> MobileEntry(string room, string code, string? subject = null, string? scene = null)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(room))
            return Content(HtmlError("Thiếu code hoặc mã phòng."), "text/html");

        var cacheKey = $"webview:{code}";
        if (!_cache.TryGetValue(cacheKey, out List<WebViewBridgeClaim>? bridgeClaims) || bridgeClaims is null)
            return Content(HtmlError("Mã xác thực hết hạn hoặc không hợp lệ.<br>Vui lòng đóng và thử lại trong ứng dụng."), "text/html");

        _cache.Remove(cacheKey); // dùng một lần

        var claims = bridgeClaims.Select(c => new Claim(c.Type, c.Value)).ToList();
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });

        return RedirectToAction(nameof(Index), new { room, subject, scene });
    }

    private static string HtmlError(string msg) =>
        $"<html><body style='font-family:sans-serif;padding:30px;background:#fff'>" +
        $"<h3 style='color:#c0392b'>⚠️ {msg}</h3>" +
        $"<p style='color:#64748b'>Đóng màn hình này và thử lại.</p>" +
        $"</body></html>";
}
