using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class DashboardController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IAttendanceService   _attendanceService;
    private readonly IFinanceService      _financeService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        INotificationService notificationService,
        IAttendanceService attendanceService,
        IFinanceService financeService,
        ILogger<DashboardController> logger)
    {
        _notificationService = notificationService;
        _attendanceService   = attendanceService;
        _financeService      = financeService;
        _logger              = logger;
    }

    // ─── GET /Dashboard ───────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var userId   = GetCurrentUserId();
        var schoolId = GetCurrentSchoolId();
        var roleCode = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Unread notification badge (shared across all roles)
        var unreadResult = await _notificationService.GetUnreadCountAsync(userId);
        ViewBag.UnreadCount = unreadResult.IsSuccess ? unreadResult.Data : 0;

        // Role-specific dashboard data
        ViewBag.Role     = roleCode;
        ViewBag.SchoolId = schoolId;

        if (roleCode == "ACCOUNTANT" || roleCode == "ADMIN")
        {
            var now = DateTime.Now;
            var report = await _financeService.GetMonthlyReportAsync(schoolId, now.Month, now.Year);
            ViewBag.FinanceReport = report.IsSuccess ? report.Data : null;
        }

        return roleCode switch
        {
            "ADMIN"      => View("Admin"),
            "TEACHER"    => View("Teacher"),
            "STUDENT"    => View("Student"),
            "PARENT"     => View("Parent"),
            "SUPERVISOR" => View("Supervisor"),
            "ACCOUNTANT" => View("Accountant"),
            _            => View()
        };
    }

    // ─── GET /Dashboard/Notifications ────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetForUserAsync(userId, page, pageSize: 20);

        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── POST /Dashboard/MarkNotificationRead ────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = GetCurrentUserId();
        await _notificationService.MarkReadAsync(userId, id);
        return Ok();
    }

    // ─── POST /Dashboard/MarkAllRead ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        await _notificationService.MarkAllReadAsync(userId);
        return Ok();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
