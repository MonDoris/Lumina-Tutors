using System.Security.Claims;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class ParentController : Controller
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IGradingService      _gradingService;
    private readonly IAttendanceService   _attendanceService;
    private readonly IClassService        _classService;
    private readonly IMessageService      _messageService;
    private readonly IUnitOfWork          _uow;
    private readonly ILogger<ParentController> _logger;

    public ParentController(
        ILeaveRequestService leaveRequestService,
        IGradingService      gradingService,
        IAttendanceService   attendanceService,
        IClassService        classService,
        IMessageService      messageService,
        IUnitOfWork          uow,
        ILogger<ParentController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _gradingService      = gradingService;
        _attendanceService   = attendanceService;
        _classService        = classService;
        _messageService      = messageService;
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
