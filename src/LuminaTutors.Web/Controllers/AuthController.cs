using System.Security.Claims;
using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

public sealed class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
    public IActionResult Login(string? returnUrl = null, string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = (role ?? "student").ToLower();
        return View();
    }

    // ─── POST /Auth/Login ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.LoginAsync(model);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Đăng nhập thất bại.");
            return View(model);
        }

        var loginData = result.Data!;
        await SignInUserAsync(loginData, model.RememberMe);

        _logger.LogInformation("User {UserId} ({Email}) signed in", loginData.UserId, loginData.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

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
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    // ─── POST /Auth/ForgotPassword ────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("email", "Vui lòng nhập địa chỉ email.");
            return View();
        }
        // TODO: integrate OTP email service; redirecting to OTP page for UI demonstration
        TempData["Info"] = $"Nếu '{email}' tồn tại trong hệ thống, mã OTP đã được gửi đến hộp thư của bạn.";
        return RedirectToAction(nameof(VerifyOtp), new { email });
    }

    // ─── GET /Auth/VerifyOtp ─────────────────────────────────────────────────

    [HttpGet]
    public IActionResult VerifyOtp(string? email = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        ViewData["Email"] = email;
        return View();
    }

    // ─── POST /Auth/VerifyOtp ─────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOtp(string otp, string? email = null)
    {
        // TODO: validate OTP against stored token; placeholder response for now
        TempData["Error"] = "Tính năng đặt lại mật khẩu qua OTP đang được hoàn thiện. Vui lòng liên hệ quản trị viên.";
        return RedirectToAction(nameof(Login));
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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, data.UserId.ToString()),
            new(ClaimTypes.Name,           data.FullName),
            new(ClaimTypes.Email,          data.Email),
            new(ClaimTypes.Role,           data.RoleCode),
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
}
