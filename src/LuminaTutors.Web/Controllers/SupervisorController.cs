using System.Security.Claims;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Supervisor (Giám thị) portal — gate checks, discipline records, messaging.
/// </summary>
[Authorize(Policy = "SupervisorAccess")]
public sealed class SupervisorController : Controller
{
    private readonly IDisciplineService    _disciplineService;
    private readonly IMessageService       _messageService;
    private readonly INotificationService  _notificationService;
    private readonly ILeaveRequestService  _leaveRequestService;
    private readonly IUnitOfWork           _uow;
    private readonly ILogger<SupervisorController> _logger;

    public SupervisorController(
        IDisciplineService    disciplineService,
        IMessageService       messageService,
        INotificationService  notificationService,
        ILeaveRequestService  leaveRequestService,
        IUnitOfWork           uow,
        ILogger<SupervisorController> logger)
    {
        _disciplineService   = disciplineService;
        _messageService      = messageService;
        _notificationService = notificationService;
        _leaveRequestService = leaveRequestService;
        _uow                 = uow;
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

        // Load parent-student-class list for the new conversation dropdown
        var parentItems = new List<ParentContactItem>();
        try
        {
            var schoolId = GetCurrentSchoolId();

            var parentRoleId = (await _uow.Roles.FindAsync(r => r.RoleCode == "PARENT")).FirstOrDefault()?.Id ?? 0;
            var parentUsers  = await _uow.Users.FindAsync(
                u => u.SchoolId == schoolId && u.RoleId == parentRoleId && u.IsActive);
            var parentUserIds = parentUsers.Select(u => u.Id).ToHashSet();

            if (parentUserIds.Any())
            {
                var relations = await _uow.ParentStudentRelations.FindAsync(
                    r => parentUserIds.Contains(r.ParentUserId),
                    include: q => q.Include(r => r.Parent).Include(r => r.Student));

                var studentIds  = relations.Select(r => r.StudentUserId).Distinct().ToList();
                var enrollments = await _uow.ClassEnrollments.FindAsync(
                    e => studentIds.Contains(e.StudentId) && e.Status == Domain.Enums.EnrollmentStatus.Active,
                    include: q => q.Include(e => e.Class));
                var enrollmentMap = enrollments
                    .GroupBy(e => e.StudentId)
                    .ToDictionary(g => g.Key, g => g.First().Class?.ClassName ?? "—");

                parentItems = relations
                    .Select(r => new ParentContactItem(
                        ParentUserId: r.ParentUserId,
                        ParentName:   r.Parent.FullName,
                        StudentName:  r.Student.FullName,
                        ClassName:    enrollmentMap.GetValueOrDefault(r.StudentUserId, "—")))
                    .OrderBy(x => x.ClassName)
                    .ThenBy(x => x.StudentName)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load parent contacts for Messages dropdown.");
        }

        ViewBag.ParentContacts = parentItems;
        return View(result.Data);
    }

    public record ParentContactItem(int ParentUserId, string ParentName, string StudentName, string ClassName);

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
        {
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(new {
                messageId   = result.Data!.MessageId,
                messageText = result.Data.MessageText,
                sentAt      = result.Data.SentAt.ToString("HH:mm dd/MM"),
                senderName  = result.Data.SenderName
            });
        }

        return RedirectToAction(nameof(Conversation), new { id = model.ConversationId });
    }

    // ─── GET /Supervisor/PollMessages?conversationId=5&afterMessageId=0 ───────

    public async Task<IActionResult> PollMessages(int conversationId, long afterMessageId)
    {
        var result = await _messageService.GetMessagesAsync(
            conversationId, GetCurrentUserId(), page: 1, pageSize: 50);

        if (!result.IsSuccess)
            return Forbid();

        var newMsgs = result.Data!.Items
            .Where(m => m.MessageId > afterMessageId)
            .OrderBy(m => m.SentAt)
            .Select(m => new {
                messageId   = m.MessageId,
                messageText = m.MessageText,
                senderName  = m.SenderName,
                isMine      = m.IsMine,
                sentAt      = m.SentAt.ToString("HH:mm"),
                isDeleted   = m.IsDeleted
            });

        return Json(newMsgs);
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
            TempData["Error"] = result.Error ?? "Không thể tạo cuộc trò chuyện. Vui lòng thử lại.";
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

    // ─── GET /Supervisor/LeaveRequests ───────────────────────────────────────

    public async Task<IActionResult> LeaveRequests(string? status, int page = 1)
    {
        var result = await _leaveRequestService.GetBySchoolAsync(
            GetCurrentSchoolId(), status, page, pageSize: 20);

        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.Status = status;
        ViewBag.Page   = page;
        return View(result.Data);
    }

    // ─── POST /Supervisor/ReviewLeaveRequest ──────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewLeaveRequest(ReviewLeaveRequestRequest model)
    {
        var result = await _leaveRequestService.ReviewAsync(
            model.RequestId, GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? (model.Approved ? "Đã chấp thuận đơn xin nghỉ." : "Đã từ chối đơn xin nghỉ.")
                : result.Error;

        return RedirectToAction(nameof(LeaveRequests));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
