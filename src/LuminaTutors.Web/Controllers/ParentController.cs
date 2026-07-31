using System.Security.Claims;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class ParentController : Controller
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IGradingService      _gradingService;
    private readonly IAttendanceService   _attendanceService;
    private readonly IClassService        _classService;
    private readonly IMessageService      _messageService;
    private readonly IFinanceService      _financeService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork          _uow;
    private readonly ILogger<ParentController> _logger;

    public ParentController(
        ILeaveRequestService leaveRequestService,
        IGradingService      gradingService,
        IAttendanceService   attendanceService,
        IClassService        classService,
        IMessageService      messageService,
        IFinanceService      financeService,
        INotificationService notificationService,
        IUnitOfWork          uow,
        ILogger<ParentController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _gradingService      = gradingService;
        _attendanceService   = attendanceService;
        _classService        = classService;
        _messageService      = messageService;
        _financeService      = financeService;
        _notificationService = notificationService;
        _uow                 = uow;
        _logger              = logger;
    }

    // ─── GET /Parent/Messages ────────────────────────────────────────────────

    public async Task<IActionResult> Messages()
    {
        var result = await _messageService.GetConversationsAsync(GetCurrentUserId());
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── GET /Parent/Conversation/5 ──────────────────────────────────────────

    public async Task<IActionResult> Conversation(int id, int page = 1)
    {
        var result = await _messageService.GetMessagesAsync(id, GetCurrentUserId(), page, pageSize: 30);
        if (!result.IsSuccess)
            return Forbid();

        ViewBag.ConversationId = id;
        return View(result.Data);
    }

    // ─── POST /Parent/SendMessage ─────────────────────────────────────────────

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

    // ─── GET /Parent/PollMessages?conversationId=5&after=638... ─────────────
    // Polling endpoint — returns new messages since a given MessageId

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

    // ─── GET /Parent/Grades ───────────────────────────────────────────────────

    public async Task<IActionResult> Grades(int? studentId, int? semesterId)
    {
        var children = await GetChildrenAsync();
        if (!children.Any())
        {
            ViewBag.Children   = children;
            ViewBag.Semesters  = new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();
            ViewBag.StudentId  = (int?)null;
            ViewBag.SemesterId = (int?)null;
            return View(null);
        }

        // Default to first child
        var selectedStudentId = studentId ?? children.First().Id;

        var semResult = await _classService.GetSemestersAsync(GetCurrentSchoolId());
        var semesters = semResult.IsSuccess ? semResult.Data! : new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();

        ViewBag.Children   = children;
        ViewBag.Semesters  = semesters;
        ViewBag.StudentId  = selectedStudentId;
        ViewBag.SemesterId = semesterId;

        if (semesterId is null)
            return View(null);

        var result = await _gradingService.GetStudentSemesterSummaryAsync(selectedStudentId, semesterId.Value);
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(null);
        }

        return View(result.Data);
    }

    // ─── GET /Parent/Attendance ───────────────────────────────────────────────

    public async Task<IActionResult> Attendance(int? studentId, int? semesterId)
    {
        var children = await GetChildrenAsync();
        if (!children.Any())
        {
            ViewBag.Children   = children;
            ViewBag.Semesters  = new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();
            ViewBag.StudentId  = (int?)null;
            ViewBag.SemesterId = (int?)null;
            return View(null);
        }

        var selectedStudentId = studentId ?? children.First().Id;

        var semResult = await _classService.GetSemestersAsync(GetCurrentSchoolId());
        var semesters = semResult.IsSuccess ? semResult.Data! : new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();

        ViewBag.Children   = children;
        ViewBag.Semesters  = semesters;
        ViewBag.StudentId  = selectedStudentId;
        ViewBag.SemesterId = semesterId;

        if (semesterId is null)
            return View(null);

        var result = await _attendanceService.GetStudentSummaryAsync(selectedStudentId, semesterId.Value);
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(null);
        }

        return View(result.Data);
    }

    // ─── GET /Parent/LeaveRequests ────────────────────────────────────────────

    public async Task<IActionResult> LeaveRequests()
    {
        var userId = GetCurrentUserId();
        var result = await _leaveRequestService.GetByParentAsync(userId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.Children = await GetChildrenAsync();
        return View(result.Data);
    }

    // ─── POST /Parent/SubmitLeaveRequest ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLeaveRequest(CreateLeaveRequestRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(LeaveRequests));
        }

        var result = await _leaveRequestService.CreateAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã gửi đơn xin nghỉ thành công." : result.Error;

        return RedirectToAction(nameof(LeaveRequests));
    }

    // ─── GET /Parent/Tuition ──────────────────────────────────────────────────
    // Hóa đơn học phí của các con: thanh toán online (VNPay) hoặc báo nộp tiền mặt.

    public async Task<IActionResult> Tuition()
    {
        var children   = await GetChildrenAsync();
        var studentIds = children.Select(c => c.Id).ToList();

        var result = await _financeService.GetInvoicesForStudentsAsync(GetCurrentSchoolId(), studentIds);

        ViewBag.Children     = children;
        ViewBag.VnPayEnabled = HttpContext.RequestServices
            .GetService<IConfiguration>()?.GetValue("VnPay:Enabled", false) ?? false;

        return View(result.IsSuccess
            ? result.Data
            : new List<LuminaTutors.Application.DTOs.Finance.InvoiceDto>());
    }

    // ─── POST /Parent/RequestCashPayment ──────────────────────────────────────
    // Phụ huynh KHÔNG tự xác nhận tiền mặt — chỉ báo nhà trường để nhân viên thu & xác nhận.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCashPayment(int invoiceId)
    {
        var schoolId   = GetCurrentSchoolId();
        var childIds   = (await GetChildrenAsync()).Select(c => c.Id).ToHashSet();

        var invoice = await _uow.TuitionInvoices.FindOneAsync(
            i => i.Id == invoiceId && i.SchoolId == schoolId,
            include: q => q.Include(i => i.Student).Include(i => i.Payments));

        // Chỉ cho phép với hóa đơn của con mình.
        if (invoice is null || !childIds.Contains(invoice.StudentId))
        {
            TempData["Error"] = "Không tìm thấy hóa đơn hợp lệ.";
            return RedirectToAction(nameof(Tuition));
        }
        if (invoice.Status == InvoiceStatus.Paid)
        {
            TempData["Error"] = "Hóa đơn đã được thanh toán.";
            return RedirectToAction(nameof(Tuition));
        }

        // Gửi thông báo tới Kế toán + Nhà trường của trường để thu tiền mặt & xác nhận.
        var staffIds = (await _uow.Users.FindAsync(
                u => u.SchoolId == schoolId && u.IsActive
                     && (u.Role.RoleCode == "ACCOUNTANT" || u.Role.RoleCode == "ADMIN"),
                include: q => q.Include(u => u.Role)))
            .Select(u => u.Id).ToList();

        var parentName = User.FindFirstValue(ClaimTypes.Name) ?? "Phụ huynh";
        var remaining  = invoice.FinalAmount - invoice.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Success).Sum(p => p.AmountPaid);

        if (staffIds.Count > 0)
        {
            await _notificationService.SendAsync(schoolId, GetCurrentUserId(), new SendNotificationRequest(
                Title: $"Phụ huynh xin nộp tiền mặt — HĐ {invoice.InvoiceCode}",
                Body: $"{parentName} muốn nộp tiền mặt {remaining:N0}đ cho học phí của {invoice.Student.FullName} " +
                      $"(hóa đơn {invoice.InvoiceCode}). Vui lòng thu tiền và xác nhận trong mục Hóa đơn học phí.",
                NotificationType: NotificationType.Tuition,
                Channel: NotificationChannel.InApp,
                TargetAudience: NotificationAudience.Specific,
                TargetUserIds: staffIds));
        }

        _logger.LogInformation("Parent {ParentId} requested cash payment for invoice {InvoiceId}",
            GetCurrentUserId(), invoiceId);

        TempData["Success"] = staffIds.Count > 0
            ? "Đã gửi yêu cầu nộp tiền mặt. Vui lòng đến văn phòng nhà trường để đóng — nhân viên sẽ xác nhận."
            : "Vui lòng đến văn phòng nhà trường để đóng tiền mặt.";
        return RedirectToAction(nameof(Tuition));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<List<ChildItem>> GetChildrenAsync()
    {
        var relations = await _uow.ParentStudentRelations.FindAsync(
            r => r.ParentUserId == GetCurrentUserId(),
            include: q => q.Include(r => r.Student));

        if (relations.Any())
            return relations.Select(r => new ChildItem(r.Student.Id, r.Student.FullName)).ToList();

        // Fallback: load all students in school (when parent-student link not yet configured by admin)
        var studentRoleId = (await _uow.Roles.FindAsync(r => r.RoleCode == "STUDENT")).FirstOrDefault()?.Id ?? 0;
        var students = await _uow.Users.FindAsync(
            u => u.SchoolId == GetCurrentSchoolId() && u.RoleId == studentRoleId && u.IsActive);
        return students.OrderBy(u => u.FullName).Select(u => new ChildItem(u.Id, u.FullName)).ToList();
    }

    public record ChildItem(int Id, string FullName);

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
