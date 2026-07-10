using System.Security.Claims;
using LuminaTutors.Application.DTOs.Support;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Kênh nhắn tin hỗ trợ: Nhà trường (ADMIN) nhắn lên Quản trị hệ thống (SYSADMIN),
/// và SYSADMIN xem/trả lời hộp thư từ tất cả các trường.
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class SupportController : Controller
{
    private readonly ISupportChatService _support;
    public SupportController(ISupportChatService support) => _support = support;

    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private int UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    // ── Nhà trường: nhắn tin với Quản trị hệ thống ────────────────────────────

    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Index()
    {
        var r = await _support.GetSchoolThreadAsync(SchoolId, markReadForSysAdmin: false);
        if (!r.IsSuccess) { TempData["Error"] = r.Error; return RedirectToAction("Index", "Dashboard"); }
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Send(string text)
    {
        var r = await _support.SendAsync(SchoolId, UserId, text ?? "");
        if (!r.IsSuccess) TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── SYSADMIN: hộp thư hỗ trợ của tất cả các trường ────────────────────────

    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Inbox()
    {
        var r = await _support.GetAllThreadsAsync();
        return View(r.IsSuccess ? r.Data : new List<SupportThreadListItemDto>());
    }

    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Thread(int schoolId)
    {
        var r = await _support.GetSchoolThreadAsync(schoolId, markReadForSysAdmin: true);
        if (!r.IsSuccess) { TempData["Error"] = r.Error; return RedirectToAction(nameof(Inbox)); }
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Reply(int schoolId, string text)
    {
        var r = await _support.SendAsync(schoolId, UserId, text ?? "");
        if (!r.IsSuccess) TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Thread), new { schoolId });
    }
}
