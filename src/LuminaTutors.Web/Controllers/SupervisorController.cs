using System.Security.Claims;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Supervisor (Giám thị) portal — gate checks, discipline records, messaging.
/// </summary>
[Authorize(Policy = "SupervisorAccess")]
public sealed class SupervisorController : Controller
{
    private readonly IDisciplineService  _disciplineService;
    private readonly IMessageService     _messageService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SupervisorController> _logger;

    public SupervisorController(
        IDisciplineService disciplineService,
        IMessageService messageService,
        INotificationService notificationService,
        ILogger<SupervisorController> logger)
    {
        _disciplineService   = disciplineService;
        _messageService      = messageService;
        _notificationService = notificationService;
        _logger              = logger;
    }

    // ─── GET /Supervisor ──────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var today  = DateOnly.FromDateTime(DateTime.Today);
        var result = await _disciplineService.GetDailyReportAsync(GetCurrentSchoolId(), today);

        return View(result.IsSuccess ? result.Data : null);
    }

    // ─── GET /Supervisor/DailyReport ──────────────────────────────────────────

    public async Task<IActionResult> DailyReport(DateOnly? date)
    {
        date ??= DateOnly.FromDateTime(DateTime.Today);
        var result = await _disciplineService.GetDailyReportAsync(GetCurrentSchoolId(), date.Value);

        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.Date = date.Value;
        return View(result.Data);
    }

    // ─── GET /Supervisor/Records ──────────────────────────────────────────────

    public async Task<IActionResult> Records(
        int? studentId, string? status, DateOnly? from, DateOnly? to, int page = 1)
    {
        var result = await _disciplineService.GetRecordsAsync(
            GetCurrentSchoolId(), studentId, status, from, to, page, pageSize: 20);

        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.StudentId = studentId;
        ViewBag.Status    = status;
        return View(result.Data);
    }

    // ─── POST /Supervisor/RecordViolation ─────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordViolation(CreateDisciplineRecordRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Records));
        }

        var result = await _disciplineService.CreateRecordAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã ghi nhận vi phạm." : result.Error;

        return RedirectToAction(nameof(Records));
    }

    // ─── POST /Supervisor/ResolveRecord ───────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveRecord(int id, string actionTaken)
    {
        var result = await _disciplineService.ResolveRecordAsync(id, actionTaken, GetCurrentUserId());

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Hồ sơ đã được xử lý." : result.Error;

        return RedirectToAction(nameof(Records));
    }

    // ─── POST /Supervisor/EscalateRecord ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EscalateRecord(int id, int escalateToUserId)
    {
        var result = await _disciplineService.EscalateRecordAsync(id, escalateToUserId);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã chuyển hồ sơ lên cấp trên." : result.Error;

        return RedirectToAction(nameof(Records));
    }

    // ─── POST /Supervisor/GateCheck ───────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GateCheck(int studentId, string checkType, bool isLate, string? note)
    {
        var result = await _disciplineService.RecordGateCheckAsync(
            GetCurrentSchoolId(), studentId, checkType,
            GetCurrentUserId(), isLate, note);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess ? Ok() : BadRequest(result.Error);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã ghi nhận kiểm tra cổng." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ─── GET /Supervisor/Messages ─────────────────────────────────────────────

    public async Task<IActionResult> Messages()
    {
        var result = await _messageService.GetConversationsAsync(GetCurrentUserId());
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── GET /Supervisor/Conversation/5 ──────────────────────────────────────

    public async Task<IActionResult> Conversation(int id, int page = 1)
    {
        var result = await _messageService.GetMessagesAsync(id, GetCurrentUserId(), page, pageSize: 30);
        if (!result.IsSuccess)
            return Forbid();

        ViewBag.ConversationId = id;
        return View(result.Data);
    }

    // ─── POST /Supervisor/SendMessage ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(SendMessageRequest model)
    {
        var result = await _messageService.SendMessageAsync(GetCurrentUserId(), model);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);

        return RedirectToAction(nameof(Conversation), new { id = model.ConversationId });
    }

    // ─── POST /Supervisor/StartConversation ───────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartConversation(StartConversationRequest model)
    {
        var result = await _messageService.StartConversationAsync(
            GetCurrentUserId(), GetCurrentSchoolId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Messages));
        }

        return RedirectToAction(nameof(Conversation), new { id = result.Data!.ConversationId });
    }

    // ─── POST /Supervisor/SendAnnouncement ────────────────────────────────────
    // Supervisor sends bulk notification to parents of a class (direct messaging)

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAnnouncement(SendNotificationRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu thông báo không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _notificationService.SendAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã gửi thông báo thành công." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
