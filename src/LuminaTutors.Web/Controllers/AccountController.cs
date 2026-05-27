using System.Security.Claims;
using LuminaTutors.Application.DTOs.Account;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        IWebHostEnvironment env,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _env            = env;
        _logger         = logger;
    }

    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    // ─── Index ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        string? role, string? keyword, bool? isActive, int page = 1)
    {
        var filter = new AccountFilterRequest(
            RoleCode: role?.ToUpper(),
            Keyword:  keyword,
            IsActive: isActive,
            Page:     page,
            PageSize: 20);

        var result = await _accountService.GetAccountsAsync(SchoolId, filter);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new AccountIndexViewModel
            {
                Accounts = new LuminaTutors.Domain.Common.PagedResult<AccountListItemDto>
                    ([], 0, 1, 20),
                Filter = filter
            });
        }

        return View(new AccountIndexViewModel
        {
            Accounts = result.Data!,
            Filter   = filter
        });
    }

    // ─── Detail ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await _accountService.GetAccountByIdAsync(SchoolId, id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(string? role)
    {
        await LoadSelectListsAsync();
        ViewBag.PresetRole = role?.ToUpper() ?? "STUDENT";
        return View(new CreateAccountRequest { RoleCode = role?.ToUpper() ?? "STUDENT" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAccountRequest model,
        IFormFile? avatar)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return View(model);
        }

        // Handle avatar upload
        string? avatarUrl = null;
        if (avatar is { Length: > 0 })
        {
            var uploadResult = await SaveAvatarAsync(avatar);
            if (uploadResult is null)
            {
                ModelState.AddModelError("avatar", "File ảnh không hợp lệ (chỉ nhận jpg/png, tối đa 5MB).");
                await LoadSelectListsAsync();
                return View(model);
            }
            avatarUrl = uploadResult;
        }

        var request = model with { AvatarUrl = avatarUrl };
        var result  = await _accountService.CreateAccountAsync(SchoolId, request);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Có lỗi khi tạo tài khoản.");
            await LoadSelectListsAsync();
            return View(model);
        }

        TempData["Success"] = $"Đã tạo tài khoản {result.Data!.FullName} thành công!";
        return RedirectToAction(nameof(Detail), new { id = result.Data.UserId });
    }

    // ─── Edit ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _accountService.GetAccountByIdAsync(SchoolId, id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Data!;
        var model = new UpdateAccountRequest
        {
            FullName              = dto.FullName,
            PhoneNumber           = dto.PhoneNumber,
            DateOfBirth           = dto.DateOfBirth,
            Gender                = dto.Gender,
            IsActive              = dto.IsActive,
            SpecializationSubject = dto.SpecializationSubject,
            Qualification         = dto.Qualification,
            ClassId               = dto.CurrentClassId,
            LinkedStudentUserId   = dto.LinkedStudentId
        };

        await LoadSelectListsAsync();
        ViewBag.AccountDetail = dto;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateAccountRequest model, IFormFile? avatar)
    {
        if (!ModelState.IsValid)
        {
            var detail = await _accountService.GetAccountByIdAsync(SchoolId, id);
            ViewBag.AccountDetail = detail.Data;
            await LoadSelectListsAsync();
            return View(model);
        }

        string? avatarUrl = null;
        if (avatar is { Length: > 0 })
        {
            avatarUrl = await SaveAvatarAsync(avatar);
            if (avatarUrl is null)
            {
                ModelState.AddModelError("avatar", "File ảnh không hợp lệ.");
                var detail = await _accountService.GetAccountByIdAsync(SchoolId, id);
                ViewBag.AccountDetail = detail.Data;
                await LoadSelectListsAsync();
                return View(model);
            }
        }

        var request = model with { AvatarUrl = avatarUrl };
        var result  = await _accountService.UpdateAccountAsync(SchoolId, id, request);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Có lỗi khi cập nhật.");
            var detail = await _accountService.GetAccountByIdAsync(SchoolId, id);
            ViewBag.AccountDetail = detail.Data;
            await LoadSelectListsAsync();
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật tài khoản thành công!";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─── Toggle Active ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _accountService.ToggleActiveAsync(SchoolId, id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã cập nhật trạng thái tài khoản." : result.Error;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─── Reset Password ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var result = await _accountService.GetAccountByIdAsync(SchoolId, id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Account = result.Data;
        return View(new AdminResetPasswordRequest(string.Empty, string.Empty));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, AdminResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
        {
            var detail = await _accountService.GetAccountByIdAsync(SchoolId, id);
            ViewBag.Account = detail.Data;
            return View(model);
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
            var detail = await _accountService.GetAccountByIdAsync(SchoolId, id);
            ViewBag.Account = detail.Data;
            return View(model);
        }

        var result = await _accountService.ResetPasswordAsync(SchoolId, id, model.NewPassword);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã đặt lại mật khẩu thành công!" : result.Error;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _accountService.DeleteAccountAsync(SchoolId, id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã vô hiệu hóa tài khoản." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task LoadSelectListsAsync()
    {
        var students = await _accountService.GetStudentSelectListAsync(SchoolId);
        var classes  = await _accountService.GetClassSelectListAsync(SchoolId);
        ViewBag.StudentList = students.IsSuccess ? students.Data : new List<(int, string, string?)>();
        ViewBag.ClassList   = classes.IsSuccess  ? classes.Data  : new List<(int, string)>();
    }

    private async Task<string?> SaveAvatarAsync(IFormFile file)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > 5 * 1024 * 1024) return null;

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/avatars/{fileName}";
    }
}

// ─── View Model ───────────────────────────────────────────────────────────────

public class AccountIndexViewModel
{
    public LuminaTutors.Domain.Common.PagedResult<AccountListItemDto> Accounts { get; set; } = null!;
    public AccountFilterRequest Filter { get; set; } = new();
}
