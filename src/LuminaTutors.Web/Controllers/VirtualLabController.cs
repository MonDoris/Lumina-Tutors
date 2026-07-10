using System.Security.Claims;
using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Web.Models;
using LuminaTutors.Web.Filters;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
[RequireFeature(PremiumFeature.VirtualLab)]
public sealed class VirtualLabController : Controller
{
    private readonly IVirtualLabService _labService;
    private readonly IAccountService   _accountService;
    private readonly IMemoryCache      _cache;
    private readonly ILogger<VirtualLabController> _logger;

    public VirtualLabController(
        IVirtualLabService labService,
        IAccountService accountService,
        IMemoryCache cache,
        ILogger<VirtualLabController> logger)
    {
        _labService     = labService;
        _accountService = accountService;
        _cache          = cache;
        _logger         = logger;
    }

    // Mapping: Subject name keywords → SubjectTag used in 3D Lab
    private static readonly (string[] Keywords, string Tag)[] SubjectTagMap =
    [
        (["hóa", "hoá", "chemistry"], "chemistry"),
        (["vật lý", "vat ly", "physics"], "physics"),
        (["sinh", "biology"], "biology"),
        (["toán", "toan", "math"], "math"),
    ];

    private static string? GuessSubjectTag(string? subjectName)
    {
        if (string.IsNullOrEmpty(subjectName)) return null;
        var lower = subjectName.ToLowerInvariant();
        foreach (var (keywords, tag) in SubjectTagMap)
            if (keywords.Any(k => lower.Contains(k)))
                return tag;
        return null;
    }

    // ─── GET /VirtualLab ──────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var result = await _labService.GetActiveSessionsAsync(GetCurrentSchoolId());
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.IsTeacher     = IsTeacher();
        ViewBag.CurrentUserId = GetCurrentUserId();

        // Auto-detect teacher's primary subject for the "Open room" form
        if (IsTeacher())
        {
            var profile = await _accountService.GetAccountByIdAsync(GetCurrentSchoolId(), GetCurrentUserId());
            if (profile.IsSuccess)
            {
                var subjectName = profile.Data!.PrimarySubjectName ?? profile.Data.SpecializationSubject;
                ViewBag.DefaultSubjectTag = GuessSubjectTag(subjectName);
            }
        }

        return View(result.Data);
    }

    // ─── POST /VirtualLab/Create ──────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLabSessionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _labService.CreateSessionAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Đã mở phòng lab. Mã tham gia: {result.Data!.SessionCode}";
        return RedirectToAction(nameof(Lab), new { id = result.Data.Id });
    }

    // ─── POST /VirtualLab/Join ────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinLabSessionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng nhập mã phòng hợp lệ (6 ký tự).";
            return RedirectToAction(nameof(Index));
        }

        var result = await _labService.GetByCodeAsync(
            GetCurrentSchoolId(), model.SessionCode.ToUpper());

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Lab), new { id = result.Data!.Id });
    }

    // ─── GET /VirtualLab/Lab/{id} ─────────────────────────────────────────────

    public async Task<IActionResult> Lab(int id)
    {
        var result = await _labService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = "Phòng lab không tồn tại hoặc đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        if (!result.Data!.IsActive)
        {
            TempData["Error"] = "Phòng lab này đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.IsHost = result.Data.TeacherId == GetCurrentUserId();

        // Bảng vẽ Toán 3D cộng tác dùng view + engine đồng bộ real-time riêng,
        // không phải scene thí nghiệm dựng sẵn của Lab.cshtml.
        if (string.Equals(result.Data.SceneType, "freedraw", StringComparison.OrdinalIgnoreCase))
            return View("MathLab", result.Data);

        return View(result.Data);
    }

    // ─── POST /VirtualLab/Close/{id} ─────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _labService.CloseSessionAsync(
            GetCurrentSchoolId(), id, GetCurrentUserId());

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã kết thúc phòng lab." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ─── GET /VirtualLab/MobileEntry?sessionCode=ABC123&token=eyJ... ─────────
    // WebView bridge: validates JWT từ mobile → ký cookie → redirect vào Lab

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> MobileEntry(string sessionCode, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sessionCode))
            return Content(HtmlError("Thiếu code hoặc mã phòng lab."), "text/html");

        var cacheKey = $"webview:{code}";
        if (!_cache.TryGetValue(cacheKey, out List<WebViewBridgeClaim>? bridgeClaims) || bridgeClaims is null)
            return Content(HtmlError("Mã xác thực hết hạn.<br>Đóng màn hình này và thử lại trong ứng dụng."), "text/html");

        _cache.Remove(cacheKey);

        var claims = bridgeClaims
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });

        var schoolId = int.Parse(claims.FirstOrDefault(c => c.Type == "SchoolId")?.Value ?? "0");

        var result = await _labService.GetByCodeAsync(schoolId, sessionCode.ToUpperInvariant());
        if (!result.IsSuccess)
            return Content(HtmlError(result.Error ?? "Không tìm thấy phòng lab."), "text/html");

        if (!result.Data!.IsActive)
            return Content(HtmlError("Phòng lab này đã kết thúc."), "text/html");

        return RedirectToAction(nameof(Lab), new { id = result.Data.Id });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int  GetCurrentUserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int  GetCurrentSchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private bool IsTeacher()          => User.IsInRole("TEACHER") || User.IsInRole("ADMIN");

    private static string HtmlError(string msg) =>
        $"<html><body style='font-family:sans-serif;padding:30px;background:#fff'>" +
        $"<h3 style='color:#c0392b'>⚠️ {msg}</h3>" +
        $"<p style='color:#64748b'>Đóng màn hình này và thử lại.</p>" +
        $"</body></html>";
}
