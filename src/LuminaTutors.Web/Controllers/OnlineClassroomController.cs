using System.Security.Claims;
using LuminaTutors.Application.DTOs.OnlineClassroom;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class OnlineClassroomController : Controller
{
    private readonly IOnlineClassroomService _service;
    private readonly ILogger<OnlineClassroomController> _logger;

    public OnlineClassroomController(
        IOnlineClassroomService service,
        ILogger<OnlineClassroomController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private string RoleCode => User.FindFirstValue(ClaimTypes.Role) ?? "";

    // ── GET /OnlineClassroom ──────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var result = await _service.GetSessionsAsync(SchoolId);
        ViewBag.Sessions = result.IsSuccess
            ? result.Data!.ToList()
            : new List<OnlineSessionListDto>();
        ViewBag.UserId = UserId;
        return View();
    }

    // ── GET /OnlineClassroom/Create ───────────────────────────────────────────

    [Authorize(Policy = "TeacherOrAdmin")]
    public IActionResult Create() => View();

    // ── POST /OnlineClassroom/Create ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Create(CreateOnlineSessionRequest req)
    {
        if (!ModelState.IsValid) return View(req);

        var result = await _service.CreateAsync(SchoolId, UserId, req);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error!);
            return View(req);
        }

        TempData["Success"] = "Phòng học đã được tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /OnlineClassroom/Edit/5 ───────────────────────────────────────────

    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _service.GetByIdAsync(SchoolId, id);
        if (!result.IsSuccess) return NotFound();
        return View(result.Data);
    }

    // ── POST /OnlineClassroom/Edit/5 ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Edit(int id, UpdateOnlineSessionRequest req)
    {
        if (!ModelState.IsValid) return View(req);

        var result = await _service.UpdateAsync(SchoolId, id, req);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error!);
            return View(req);
        }

        TempData["Success"] = "Cập nhật phòng học thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClassroom/Delete/5 ────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(SchoolId, id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa phòng học." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /OnlineClassroom/Start/5 ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Start(int id)
    {
        var result = await _service.StartSessionAsync(SchoolId, id, UserId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Room), new { id });
    }

    // ── POST /OnlineClassroom/End/5 ───────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> End(int id)
    {
        var result = await _service.EndSessionAsync(SchoolId, id, UserId);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Phòng học đã kết thúc." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /OnlineClassroom/Room/5 ───────────────────────────────────────────

    public async Task<IActionResult> Room(int id)
    {
        var result = await _service.GetByIdAsync(SchoolId, id);
        if (!result.IsSuccess) return NotFound();

        ViewBag.Session   = result.Data!;
        ViewBag.UserId    = UserId;
        ViewBag.IsHost    = result.Data!.TeacherId == UserId;
        ViewBag.UserName  = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
        return View();
    }

    // ── GET /OnlineClassroom/Join ─────────────────────────────────────────────
    // Page for entering room code

    public IActionResult Join() => View();

    // ── POST /OnlineClassroom/JoinByCode ─────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinByCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            TempData["Error"] = "Vui lòng nhập mã phòng.";
            return RedirectToAction(nameof(Join));
        }

        var result = await _service.JoinByCodeAsync(SchoolId, UserId, roomCode);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Join));
        }

        return RedirectToAction(nameof(Room), new { id = result.Data!.Session.Id });
    }

    // ── POST /OnlineClassroom/UploadSlide ─────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> UploadSlide(int sessionId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File không hợp lệ." });

        // Save to wwwroot/uploads/slides/
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slides");
        Directory.CreateDirectory(uploadsDir);

        var ext      = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var fileUrl    = $"/uploads/slides/{fileName}";
        var totalPages = 1; // Default; client can update after PDF render

        var result = await _service.UploadSlideAsync(sessionId, file.FileName, fileUrl, totalPages);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // ── DELETE /OnlineClassroom/DeleteSlide ───────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> DeleteSlide(int sessionId, int slideId)
    {
        var result = await _service.DeleteSlideAsync(sessionId, slideId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { success = true });
    }

    // ── GET /OnlineClassroom/Participants/5 ───────────────────────────────────

    public async Task<IActionResult> Participants(int sessionId)
    {
        var result = await _service.GetParticipantsAsync(sessionId);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Json(result.Data);
    }
}
