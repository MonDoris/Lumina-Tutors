using System.Linq;
using System.Security.Claims;
using LuminaTutors.Application.Common;
using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace LuminaTutors.Web.Controllers;

public sealed class AuthController : Controller
{
    private readonly IAuthService            _authService;
    private readonly IMemoryCache            _cache;
    private readonly IHostEnvironment        _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService            authService,
        IMemoryCache            cache,
        IHostEnvironment        env,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _cache       = cache;
        _env         = env;
        _logger      = logger;
    }

    // ─── GET /Auth/SelectRole ─────────────────────────────────────────────────

    [HttpGet("/auth/chon-vai-tro")]
    [HttpGet("Auth/SelectRole")]
    public IActionResult SelectRole()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    // ─── GET /Auth/Login ──────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? role = null, string? ui = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = NormalizeRole(role);

        // MẶC ĐỊNH: concept "Quantum Access Terminal".
        // Bản luxury cũ vẫn giữ nguyên, xem qua: /Auth/Login?ui=luxury
        if (string.Equals(ui, "luxury", StringComparison.OrdinalIgnoreCase))
            return View("Login");

        return View("LoginQuantum");
    }

    // ─── POST /Auth/Login ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null, string? role = null)
    {
        // Giữ lại role để view hiển thị đúng form khi có lỗi
        ViewBag.Role = NormalizeRole(role);

        // Khi lỗi: ở lại đúng giao diện mặc định (Quantum Access Terminal)
        if (!ModelState.IsValid)
            return View("LoginQuantum", model);

        var result = await _authService.LoginAsync(model);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Email hoặc mật khẩu không đúng.");
            return View("LoginQuantum", model);
        }

        var loginData = result.Data!;

        // Cổng vai trò phải khớp vai trò thật của tài khoản — chọn nhầm cổng thì từ chối.
        var expectedTab = loginData.RoleCode switch
        {
            "STUDENT"    => "student",
            "TEACHER"    => "teacher",
            "SUPERVISOR" => "supervisor",
            "PARENT"     => "parent",
            "ADMIN"      => "admin",
            _            => null      // SYSADMIN / ACCOUNTANT: không có cổng riêng → cho qua
        };
        var selectedTab = NormalizeRole(role);
        if (expectedTab is not null && selectedTab != expectedTab)
        {
            ModelState.AddModelError(string.Empty,
                $"Tài khoản này là {RoleDisplayName(expectedTab)}, không phải {RoleDisplayName(selectedTab)}. Vui lòng chọn đúng cổng truy cập.");
            _logger.LogWarning("Đăng nhập sai cổng: {Email} ({Role}) chọn cổng {Tab}", loginData.Email, loginData.RoleCode, selectedTab);
            return View("LoginQuantum", model);
        }

        // Đối chiếu đuôi (subdomain) email với vai trò thật — email mang tiền tố sai thì từ chối.
        var emailRole = RoleEmail.RoleOf(model.Email);
        if (emailRole is not null && emailRole != loginData.RoleCode)
        {
            ModelState.AddModelError(string.Empty,
                "Email không khớp vai trò tài khoản. Vui lòng dùng đúng địa chỉ đã được cấp.");
            return View("LoginQuantum", model);
        }

        await SignInUserAsync(loginData, model.RememberMe);

        _logger.LogInformation("User {UserId} ({Email}) signed in", loginData.UserId, loginData.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // SYSADMIN (bên bán) vào thẳng khu E-Selling; các vai trò khác về Dashboard trường
        if (loginData.RoleCode == "SYSADMIN")
            return RedirectToAction("Subscriptions", "Subscription");

        return RedirectToAction("Index", "Dashboard");
    }

    // ─── POST /Auth/Logout ────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAsync(userId, string.Empty);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {UserId} signed out", userId);
        return RedirectToAction(nameof(Login));
    }

    // ─── GET /Auth/ForgotPassword ─────────────────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword(string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.Role = NormalizeRole(role);
        return View();
    }

    // ─── POST /Auth/ForgotPassword ────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string phone, string? role = null)
    {
        role = NormalizeRole(role);
        ViewBag.Role = role;

        var normalized = NormalizePhone(phone ?? string.Empty);
        if (normalized.Length < 9)
        {
            ViewData["PhoneError"] = "Vui lòng nhập số điện thoại hợp lệ (ít nhất 9 chữ số).";
            return View();
        }

        var lookup = await _authService.FindUserByPhoneAsync(normalized);
        if (!lookup.IsSuccess)
        {
            ViewData["PhoneError"] = lookup.Error;
            return View();
        }

        // Generate 6-digit OTP and store in cache (5-minute TTL)
        var otp   = Random.Shared.Next(100_000, 999_999).ToString();
        var entry = new OtpEntry { Code = otp, ExpiresAt = DateTime.UtcNow.AddMinutes(5) };
        _cache.Set($"fp:otp:{normalized}", entry, TimeSpan.FromMinutes(5));

        // DEV: log OTP so testers can see it (remove in production)
        _logger.LogWarning("[DEV] OTP for {Phone} → {Otp}", MaskPhone(normalized), otp);
        if (_env.IsDevelopment()) TempData["OtpDev"] = otp;

        TempData["MaskedPhone"] = lookup.Data;
        return RedirectToAction(nameof(VerifyOtp), new { phone = normalized, role });
    }

    // ─── GET /Auth/VerifyOtp ─────────────────────────────────────────────────

    [HttpGet]
    public IActionResult VerifyOtp(string? phone = null, string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.Role        = NormalizeRole(role);
        ViewBag.Phone       = phone ?? string.Empty;
        ViewBag.MaskedPhone = TempData["MaskedPhone"];
        if (_env.IsDevelopment()) ViewBag.OtpDev = TempData["OtpDev"];
        return View();
    }

    // ─── POST /Auth/VerifyOtp ─────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOtp(string phone, string otp, string? role = null)
    {
        role = NormalizeRole(role);
        ViewBag.Role  = role;
        ViewBag.Phone = phone;

        var normalized = NormalizePhone(phone ?? string.Empty);

        if (!_cache.TryGetValue<OtpEntry>($"fp:otp:{normalized}", out var entry) || entry is null)
        {
            TempData["Error"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu lại.";
            return RedirectToAction(nameof(ForgotPassword), new { role });
        }

        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _cache.Remove($"fp:otp:{normalized}");
            TempData["Error"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu lại.";
            return RedirectToAction(nameof(ForgotPassword), new { role });
        }

        entry.Attempts++;
        if (entry.Attempts > 5)
        {
            _cache.Remove($"fp:otp:{normalized}");
            TempData["Error"] = "Quá nhiều lần nhập sai. Vui lòng yêu cầu mã mới.";
            return RedirectToAction(nameof(ForgotPassword), new { role });
        }

        var remaining = TimeSpan.FromSeconds((entry.ExpiresAt - DateTime.UtcNow).TotalSeconds);

        if (entry.Code != (otp ?? string.Empty).Trim())
        {
            _cache.Set($"fp:otp:{normalized}", entry, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromSeconds(1));
            ViewData["OtpError"] = $"Mã OTP không đúng. Còn {5 - entry.Attempts} lần thử.";
            return View();
        }

        // OTP valid — issue reset token (10-minute TTL)
        _cache.Remove($"fp:otp:{normalized}");
        var resetToken = Guid.NewGuid().ToString("N");
        _cache.Set($"fp:reset:{resetToken}", normalized, TimeSpan.FromMinutes(10));

        return RedirectToAction(nameof(ResetPassword), new { token = resetToken, role });
    }

    // ─── GET /Auth/ResetPassword ──────────────────────────────────────────────

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        role = NormalizeRole(role);

        if (string.IsNullOrWhiteSpace(token) ||
            !_cache.TryGetValue<string>($"fp:reset:{token}", out _))
        {
            TempData["Error"] = "Phiên đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(ForgotPassword), new { role });
        }

        ViewBag.Role  = role;
        ViewBag.Token = token;
        return View();
    }

    // ─── POST /Auth/ResetPassword ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        string token, string newPassword, string confirmPassword, string? role = null)
    {
        role = NormalizeRole(role);
        ViewBag.Role  = role;
        ViewBag.Token = token;

        if (newPassword != confirmPassword)
        {
            ViewData["PwError"] = "Mật khẩu xác nhận không khớp.";
            return View();
        }

        if ((newPassword ?? string.Empty).Length < 6)
        {
            ViewData["PwError"] = "Mật khẩu phải có ít nhất 6 ký tự.";
            return View();
        }

        if (!_cache.TryGetValue<string>($"fp:reset:{token}", out var normalizedPhone)
            || string.IsNullOrEmpty(normalizedPhone))
        {
            TempData["Error"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng thử lại.";
            return RedirectToAction(nameof(ForgotPassword), new { role });
        }

        var result = await _authService.ResetPasswordByPhoneAsync(normalizedPhone, newPassword!);
        if (!result.IsSuccess)
        {
            ViewData["PwError"] = result.Error;
            return View();
        }

        _cache.Remove($"fp:reset:{token}");
        _logger.LogInformation("Password reset completed for phone {Phone}", MaskPhone(normalizedPhone));

        TempData["Success"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login), new { role });
    }

    // ─── GET /Auth/AccessDenied ───────────────────────────────────────────────

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ─── GET /Auth/ChangePassword ─────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "AnyAuthenticated")]
    public IActionResult ChangePassword() => View();

    // ─── POST /Auth/ChangePassword ────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AnyAuthenticated")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ChangePasswordAsync(GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Thay đổi mật khẩu thất bại.");
            return View(model);
        }

        TempData["Success"] = "Mật khẩu đã được thay đổi thành công.";
        return RedirectToAction("Index", "Dashboard");
    }

    // ─── GET /Auth/Activate?token=GUID ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Activate(Guid token)
    {
        var result = await _authService.GetInviteLinkByTokenAsync(token);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Liên kết không hợp lệ.";
            return View("InvalidInvite");
        }

        ViewBag.Token     = token;
        ViewBag.InviteDto = result.Data;
        return View(new ActivateInviteRequest(token, string.Empty, string.Empty, string.Empty, string.Empty));
    }

    // ─── POST /Auth/Activate ──────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ActivateInviteRequest model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Token = model.Token;
            return View(model);
        }

        var result = await _authService.ActivateInviteAsync(model);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kích hoạt thất bại.");
            ViewBag.Token = model.Token;
            return View(model);
        }

        // Auto sign-in after activation
        await SignInUserAsync(result.Data!, rememberMe: false);
        TempData["Success"] = "Tài khoản đã được kích hoạt. Chào mừng bạn!";
        return RedirectToAction("Index", "Dashboard");
    }

    // ─── GET /Auth/InviteLinks (Admin only) ──────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> InviteLinks(int page = 1, int pageSize = 20)
    {
        var schoolId = GetCurrentSchoolId();
        var result   = await _authService.GetInviteLinksAsync(schoolId, page, pageSize);

        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── POST /Auth/CreateInviteLink ──────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInviteLink(CreateInviteLinkRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(InviteLinks));
        }

        var result = await _authService.CreateInviteLinkAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(InviteLinks));
        }

        TempData["Success"] = $"Đã tạo liên kết mời. Token: {result.Data!.Token}";
        return RedirectToAction(nameof(InviteLinks));
    }

    // ─── POST /Auth/RevokeInviteLink ──────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeInviteLink(int id)
    {
        var result = await _authService.RevokeInviteLinkAsync(id, GetCurrentUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã thu hồi liên kết." : result.Error;

        return RedirectToAction(nameof(InviteLinks));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task SignInUserAsync(LoginResponse data, bool rememberMe)
    {
        // Quản trị hệ thống (SYSADMIN) là vai trò "bên bán" thuần: chỉ giữ claim
        // "SYSADMIN" → chỉ qua được policy SystemAdmin (quản lý gói E-Selling), KHÔNG
        // có quyền các chức năng quản trị trường (AdminOnly, FinanceAccess, …).
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, data.UserId.ToString()),
            new(ClaimTypes.Name,           data.FullName),
            new(ClaimTypes.Email,          data.Email),
            new(ClaimTypes.Role,           data.RoleCode),
            new("RoleName",                data.RoleName ?? data.RoleCode),
            new("SchoolId",                data.SchoolId.ToString()),
            new("SchoolName",              data.SchoolName),
            new("AvatarUrl",               data.AvatarUrl ?? string.Empty)
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc   = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProps);
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());

    private static string MaskPhone(string phone) =>
        phone.Length >= 6 ? phone[..3] + "****" + phone[^2..] : "****";

    // Whitelist vai trò dùng cho theming các trang Auth (Login/ForgotPassword/VerifyOtp/ResetPassword).
    // `role` đến từ query string nên phải chốt về tập giá trị an toàn đã biết — vừa chống XSS reflected
    // (defense-in-depth), vừa tránh giá trị lạ làm vỡ giao diện theo vai trò. Khớp whitelist trong các view Login.
    private static readonly string[] _validAuthRoles = { "student", "teacher", "supervisor", "parent", "admin" };

    private static string NormalizeRole(string? role)
    {
        var normalized = (role ?? "student").Trim().ToLowerInvariant();
        return _validAuthRoles.Contains(normalized) ? normalized : "student";
    }

    private static string RoleDisplayName(string tabRole) => tabRole switch
    {
        "student"    => "Học Sinh",
        "teacher"    => "Giáo Viên",
        "supervisor" => "Giám Thị",
        "parent"     => "Phụ Huynh",
        "admin"      => "Nhà Trường",
        _            => "người dùng"
    };
}
