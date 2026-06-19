using System.Security.Claims;
using LuminaTutors.Application.DTOs.AI;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class AiTutorController : Controller
{
    private readonly IAiTutorService           _aiTutor;
    private readonly ILogger<AiTutorController> _logger;

    public AiTutorController(IAiTutorService aiTutor, ILogger<AiTutorController> logger)
    {
        _aiTutor = aiTutor;
        _logger  = logger;
    }

    // ─── Student: danh sách phiên ─────────────────────────────────────────────

    [RequireFeature(PremiumFeature.AiTutor)]
    public async Task<IActionResult> Index()
    {
        var result = await _aiTutor.GetStudentSessionsAsync(SchoolId(), UserId());
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new List<AiTutorSessionDto>());
        }
        return View(result.Data);
    }

    // ─── Student: tạo phiên mới ───────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, RequireFeature(PremiumFeature.AiTutor)]
    public async Task<IActionResult> Create(string title)
    {
        var result = await _aiTutor.CreateSessionAsync(SchoolId(), UserId(), title);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Chat), new { id = result.Data!.SessionId });
    }

    // ─── Student: giao diện trò chuyện ────────────────────────────────────────

    [RequireFeature(PremiumFeature.AiTutor)]
    public async Task<IActionResult> Chat(int id)
    {
        var sessionResult = await _aiTutor.GetSessionAsync(SchoolId(), id, UserId());
        if (!sessionResult.IsSuccess) return NotFound();

        var msgResult = await _aiTutor.GetMessagesAsync(id);
        ViewBag.Session  = sessionResult.Data;
        ViewBag.Messages = msgResult.IsSuccess ? msgResult.Data : new List<AiTutorMessageDto>();
        return View();
    }

    // ─── Student: gửi tin nhắn (AJAX) ────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, RequireFeature(PremiumFeature.AiTutor)]
    public async Task<IActionResult> Send(int sessionId, [FromBody] SendMessageRequest request)
    {
        // request có thể null nếu body rỗng/sai content-type ([FromBody] không bind được) → tránh NRE.
        // Đồng thời chặn nội dung rỗng ngay tại đây để khỏi gọi AI lãng phí token.
        if (request is null || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Tin nhắn không được để trống." });

        var result = await _aiTutor.SendMessageAsync(SchoolId(), sessionId, UserId(), request.Content);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            messageId = result.Data!.MessageId,
            role      = result.Data.Role,
            content   = result.Data.Content,
            createdAt = result.Data.CreatedAt.ToString("HH:mm")
        });
    }

    // ─── Student: xóa phiên ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _aiTutor.DeleteSessionAsync(SchoolId(), id, UserId());
        return RedirectToAction(nameof(Index));
    }

    // ─── Admin: danh sách tất cả phiên ────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminIndex(bool? flaggedOnly)
    {
        var result = await _aiTutor.GetAllSessionsAsync(SchoolId(), flaggedOnly);
        ViewBag.FlaggedOnly = flaggedOnly;
        return View(result.IsSuccess ? result.Data : new List<AiTutorSessionDto>());
    }

    // ─── Admin: xem chi tiết phiên ────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminSession(int id)
    {
        var sessionResult = await _aiTutor.GetSessionAsync(SchoolId(), id, UserId());
        if (!sessionResult.IsSuccess) return NotFound();

        var msgResult = await _aiTutor.GetMessagesAsync(id);
        ViewBag.Session  = sessionResult.Data;
        ViewBag.Messages = msgResult.IsSuccess ? msgResult.Data : new List<AiTutorMessageDto>();
        return View();
    }

    // ─── Admin: ghi chú / gắn cờ phiên ──────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminNote(int sessionId, [FromBody] AdminNoteRequest request)
    {
        var result = await _aiTutor.UpdateAdminNoteAsync(sessionId, request.Note, request.IsFlagged);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok();
    }

    // ─── Admin: gắn cờ / bỏ cờ tin nhắn ─────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> FlagMessage(int messageId, [FromBody] FlagMessageRequest request)
    {
        var result = await _aiTutor.FlagMessageAsync(messageId, request.IsFlagged);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok();
    }

    // ─── Admin: đánh dấu đã duyệt ────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> MarkReviewed(int sessionId)
    {
        await _aiTutor.MarkSessionReviewedAsync(sessionId);
        return RedirectToAction(nameof(AdminSession), new { id = sessionId });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private int SchoolId() => int.Parse(User.FindFirstValue("SchoolId")                ?? "0");
    private int UserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
