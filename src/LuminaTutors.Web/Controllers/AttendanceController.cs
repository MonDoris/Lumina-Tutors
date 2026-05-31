using System.Security.Claims;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger            = logger;
    }

    // ─── GET /Attendance ──────────────────────────────────────────────────────
    // Teacher's active sessions list

    public IActionResult Index() => View();

    // ─── GET /Attendance/Session/5 ────────────────────────────────────────────

    public async Task<IActionResult> Session(int id)
    {
        var result = await _attendanceService.GetSessionAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Data);
    }

    // ─── POST /Attendance/Create ──────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSessionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _attendanceService.CreateSessionAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã mở phiên điểm danh. Học sinh có thể quét mã QR.";
        return RedirectToAction(nameof(Session), new { id = result.Data!.SessionId });
    }

    // ─── POST /Attendance/Close/5 ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _attendanceService.CloseSessionAsync(id, GetCurrentUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã đóng phiên điểm danh." : result.Error;

        return RedirectToAction(nameof(Session), new { id });
    }

    // ─── GET /Attendance/QR/5 ─────────────────────────────────────────────────
    // Display QR code for students to scan

    public async Task<IActionResult> QR(int id)
    {
        var result = await _attendanceService.GetSessionAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        // QR content = absolute URL to scan endpoint + token
        var qrContent = Url.Action("Scan", "Attendance",
            new { token = result.Data!.QRToken }, Request.Scheme)!;

        ViewBag.QRContent  = qrContent;
        ViewBag.ExpiresAt  = result.Data.QRExpiresAt;
        return View(result.Data);
    }

    // ─── GET /Attendance/Scan?token=GUID ─────────────────────────────────────
    // Student scans QR → server validates and records attendance

    [Authorize(Policy = "AnyAuthenticated")]
    [HttpGet]
    public IActionResult Scan(Guid token)
    {
        ViewBag.Token = token;
        return View();
    }

    // ─── POST /Attendance/Scan ────────────────────────────────────────────────

    [Authorize(Policy = "AnyAuthenticated")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan(ScanQRRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _attendanceService.ScanQRAsync(model);

        ViewBag.ScanResult = result;
        return View("ScanResult", result);
    }

    // ─── POST /Attendance/UpdateRecord ────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRecord(int sessionId, UpdateAttendanceRequest model)
    {
        var result = await _attendanceService.UpdateAttendanceAsync(sessionId, GetCurrentUserId(), model);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess ? Ok() : BadRequest(result.Error);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Cập nhật điểm danh thành công." : result.Error;

        return RedirectToAction(nameof(Session), new { id = sessionId });
    }

    // ─── GET /Attendance/DailyReport ─────────────────────────────────────────

    public async Task<IActionResult> DailyReport(int classId, DateOnly? date)
    {
        date ??= DateOnly.FromDateTime(DateTime.Today);
        var result = await _attendanceService.GetDailyReportAsync(classId, date.Value);

        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── GET /Attendance/StudentSummary ───────────────────────────────────────

    public async Task<IActionResult> StudentSummary(int studentId, int semesterId)
    {
        var result = await _attendanceService.GetStudentSummaryAsync(studentId, semesterId);
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── POST /Attendance/NotifyParents ───────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotifyParents(int sessionId)
    {
        var result = await _attendanceService.NotifyAbsentParentsAsync(sessionId);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã gửi thông báo đến {result.Data} phụ huynh."
                : result.Error;

        return RedirectToAction(nameof(Session), new { id = sessionId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
