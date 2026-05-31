using System.Security.Claims;
using LuminaTutors.Application.DTOs.Account;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class DashboardController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IAttendanceService   _attendanceService;
    private readonly IFinanceService      _financeService;
    private readonly IAccountService      _accountService;
    private readonly IClassService        _classService;
    private readonly IUnitOfWork          _uow;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        INotificationService notificationService,
        IAttendanceService attendanceService,
        IFinanceService financeService,
        IAccountService accountService,
        IClassService classService,
        IUnitOfWork uow,
        ILogger<DashboardController> logger)
    {
        _notificationService = notificationService;
        _attendanceService   = attendanceService;
        _financeService      = financeService;
        _accountService      = accountService;
        _classService        = classService;
        _uow                 = uow;
        _logger              = logger;
    }

    // ─── GET /Dashboard ───────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var userId   = GetCurrentUserId();
        var schoolId = GetCurrentSchoolId();
        var roleCode = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        ViewBag.Role     = roleCode;
        ViewBag.SchoolId = schoolId;

        // Unread notification badge
        var unreadResult = await _notificationService.GetUnreadCountAsync(userId);
        ViewBag.UnreadCount = unreadResult.IsSuccess ? unreadResult.Data : 0;

        // ── ADMIN ─────────────────────────────────────────────────────────────
        if (roleCode == "ADMIN")
        {
            var now = DateTime.Now;

            // Counts
            var students = await _accountService.GetAccountsAsync(schoolId,
                new AccountFilterRequest(RoleCode: "STUDENT", Page: 1, PageSize: 1));
            ViewBag.TotalStudents = students.IsSuccess ? students.Data!.TotalCount : 0;

            var teachers = await _accountService.GetAccountsAsync(schoolId,
                new AccountFilterRequest(RoleCode: "TEACHER", Page: 1, PageSize: 1));
            ViewBag.TotalTeachers = teachers.IsSuccess ? teachers.Data!.TotalCount : 0;

            var years = await _classService.GetAcademicYearsAsync(schoolId);
            if (years.IsSuccess && years.Data!.Count > 0)
            {
                var activeYear = years.Data.FirstOrDefault(y => y.IsActive) ?? years.Data[0];
                var classes    = await _classService.GetAllAsync(schoolId, activeYear.AcademicYearId);
                ViewBag.TotalClasses   = classes.IsSuccess ? classes.Data!.Count : 0;
                ViewBag.ActiveYearName = activeYear.YearName;
            }
            else { ViewBag.TotalClasses = 0; ViewBag.ActiveYearName = ""; }

            // Finance
            var report = await _financeService.GetMonthlyReportAsync(schoolId, now.Month, now.Year);
            if (report.IsSuccess && report.Data != null)
            {
                ViewBag.MonthRevenue  = report.Data.TotalCollected;
                ViewBag.OverdueCount  = report.Data.OverdueInvoices;
                ViewBag.OverdueAmount = report.Data.TotalOutstanding;
                ViewBag.CollectionRate = report.Data.CollectionRate;
            }
            else { ViewBag.MonthRevenue = 0m; ViewBag.OverdueCount = 0; ViewBag.OverdueAmount = 0m; ViewBag.CollectionRate = 0m; }

            // Subjects
            var subjects = await _uow.Subjects.FindAsync(s => s.SchoolId == schoolId);
            ViewBag.TotalSubjects = subjects.Count;
            ViewBag.ActiveSubjects = subjects.Count(s => s.IsActive);
            ViewBag.RecentSubjects = subjects.OrderByDescending(s => s.Id).Take(5).ToList();

            // Online sessions
            var sessions = await _uow.OnlineSessions.FindAsync(s => s.SchoolId == schoolId);
            ViewBag.TotalSessions = sessions.Count;
            ViewBag.LiveSessions  = sessions.Count(s => s.Status == LuminaTutors.Domain.Enums.OnlineSessionStatus.Live);

            // Recent accounts
            var recent = await _accountService.GetAccountsAsync(schoolId,
                new AccountFilterRequest(Page: 1, PageSize: 5));
            ViewBag.RecentAccounts = recent.IsSuccess ? recent.Data!.Items.ToList() : new List<AccountListItemDto>();
        }

        // ── TEACHER ───────────────────────────────────────────────────────────
        if (roleCode == "TEACHER")
        {
            var years = await _classService.GetAcademicYearsAsync(schoolId);
            if (years.IsSuccess && years.Data!.Count > 0)
            {
                var activeYear = years.Data.FirstOrDefault(y => y.IsActive) ?? years.Data[0];
                var classes    = await _classService.GetAllAsync(schoolId, activeYear.AcademicYearId);
                ViewBag.TotalClasses   = classes.IsSuccess ? classes.Data!.Count : 0;
                ViewBag.ActiveYearName = activeYear.YearName;
            }
            else { ViewBag.TotalClasses = 0; }

            var students = await _accountService.GetAccountsAsync(schoolId,
                new AccountFilterRequest(RoleCode: "STUDENT", Page: 1, PageSize: 1));
            ViewBag.TotalStudents = students.IsSuccess ? students.Data!.TotalCount : 0;

            // Teacher's sessions
            var sessions = await _uow.OnlineSessions.FindAsync(
                s => s.SchoolId == schoolId && s.TeacherId == userId);
            ViewBag.TotalSessions = sessions.Count;
            ViewBag.RecentSessions = sessions.OrderByDescending(s => s.CreatedAt).Take(3).ToList();
        }

        // ── ACCOUNTANT ────────────────────────────────────────────────────────
        if (roleCode == "ACCOUNTANT")
        {
            var now    = DateTime.Now;
            var report = await _financeService.GetMonthlyReportAsync(schoolId, now.Month, now.Year);
            if (report.IsSuccess && report.Data != null)
            {
                ViewBag.MonthRevenue   = report.Data.TotalCollected;
                ViewBag.OverdueCount   = report.Data.OverdueInvoices;
                ViewBag.OverdueAmount  = report.Data.TotalOutstanding;
                ViewBag.PaidInvoices   = report.Data.PaidInvoices;
                ViewBag.TotalInvoices  = report.Data.TotalInvoices;
                ViewBag.CollectionRate = report.Data.CollectionRate;
            }
        }

        // ── STUDENT ───────────────────────────────────────────────────────────
        if (roleCode == "STUDENT")
        {
            var unread = await _notificationService.GetUnreadCountAsync(userId);
            ViewBag.StudentNotifications = unread.IsSuccess ? unread.Data : 0;
        }

        // ── PARENT ────────────────────────────────────────────────────────────
        if (roleCode == "PARENT")
        {
            var unread = await _notificationService.GetUnreadCountAsync(userId);
            ViewBag.ParentNotifications = unread.IsSuccess ? unread.Data : 0;
        }

        // ── SUPERVISOR ────────────────────────────────────────────────────────
        if (roleCode == "SUPERVISOR")
        {
            var years = await _classService.GetAcademicYearsAsync(schoolId);
            if (years.IsSuccess && years.Data!.Count > 0)
            {
                var activeYear = years.Data.FirstOrDefault(y => y.IsActive) ?? years.Data[0];
                var classes    = await _classService.GetAllAsync(schoolId, activeYear.AcademicYearId);
                ViewBag.TotalClasses = classes.IsSuccess ? classes.Data!.Count : 0;
            }
            else { ViewBag.TotalClasses = 0; }

            var students = await _accountService.GetAccountsAsync(schoolId,
                new AccountFilterRequest(RoleCode: "STUDENT", Page: 1, PageSize: 1));
            ViewBag.TotalStudents = students.IsSuccess ? students.Data!.TotalCount : 0;
        }

        return roleCode switch
        {
            "ADMIN"      => View("Admin"),
            "TEACHER"    => View("Teacher"),
            "STUDENT"    => View("Student"),
            "PARENT"     => View("Parent"),
            "SUPERVISOR" => View("Supervisor"),
            "ACCOUNTANT" => View("Accountant"),
            _            => View("Admin")
        };
    }

    // ─── GET /Dashboard/Notifications ────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetForUserAsync(userId, page, pageSize: 20);
        if (!result.IsSuccess) return StatusCode(500);
        return View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        await _notificationService.MarkReadAsync(GetCurrentUserId(), id);
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notificationService.MarkAllReadAsync(GetCurrentUserId());
        return Ok();
    }

    private int GetCurrentUserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int GetCurrentSchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
