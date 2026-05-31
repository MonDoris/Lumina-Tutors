using System.Security.Claims;
using LuminaTutors.Application.DTOs.Online;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class OnlineClassController : Controller
{
    private readonly IOnlineClassService _svc;

    public OnlineClassController(IOnlineClassService svc) => _svc = svc;

    // ── GET /OnlineClass ──────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var result = await _svc.GetSessionsAsync(GetSchoolId(), GetUserId(), GetRole());
        ViewBag.Role   = GetRole();
        ViewBag.UserId = GetUserId();
        return View(result.IsSuccess ? result.Data : null);
    }

    // ── GET /OnlineClass/Create ───────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpGet]
    public IActionResult Create() => View();

    // ── POST /OnlineClass/Create ──────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title, string? description,
        DateTime? scheduledAt, int maxParticipants = 50)
    {
        var req    = new CreateOnlineSessionRequest(title, description, scheduledAt, maxParticipants);
        var result = await _svc.CreateAsync(GetSchoolId(), GetUserId(), req);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = $"Đã tạo phòng học \"{result.Data!.Title}\". Mã: {result.Data.RoomCode}";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClass/Start/5 ─────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var result = await _svc.StartAsync(GetSchoolId(), id, GetUserId());
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else
        {
            // Redirect to the room immediately
            return RedirectToAction(nameof(Room), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    // ── GET /OnlineClass/Room/5 ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Room(int id)
    {
        var joinResult = await _svc.JoinAsync(GetSchoolId(), id, GetUserId());

        if (!joinResult.IsSuccess)
        {
            TempData["Error"] = joinResult.Error;
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserName  = User.FindFirstValue(ClaimTypes.Name) ?? "Ẩn danh";
        ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
        ViewBag.Role      = GetRole();
        return View(joinResult.Data);
    }

    // ── POST /OnlineClass/Leave ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int sessionId)
    {
        await _svc.LeaveAsync(sessionId, GetUserId());
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClass/End/5 ───────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> End(int id)
    {
        var result = await _svc.EndAsync(GetSchoolId(), id, GetUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã kết thúc phòng học." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClass/Cancel/5 ────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _svc.CancelAsync(GetSchoolId(), id, GetUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã hủy phòng học." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClass/Delete/5 ────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _svc.DeleteAsync(GetSchoolId(), id, GetUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa phòng học." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /OnlineClass/Participants/5 (JSON for AJAX) ───────────────────────
    [HttpGet]
    public async Task<IActionResult> Participants(int id)
    {
        var result = await _svc.GetParticipantsAsync(id);
        return result.IsSuccess ? Json(result.Data) : BadRequest();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int    GetUserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int    GetSchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private string GetRole()     => User.FindFirstValue(ClaimTypes.Role) ?? "";
}
