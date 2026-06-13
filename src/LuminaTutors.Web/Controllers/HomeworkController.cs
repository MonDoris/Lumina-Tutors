using System.Security.Claims;
using LuminaTutors.Application.DTOs.Homework;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class HomeworkController : Controller
{
    private readonly IHomeworkService _svc;
    private readonly IAccountService  _accountSvc;
    private readonly ILogger<HomeworkController> _logger;

    public HomeworkController(
        IHomeworkService svc,
        IAccountService accountSvc,
        ILogger<HomeworkController> logger)
    {
        _svc        = svc;
        _accountSvc = accountSvc;
        _logger     = logger;
    }

    private int    UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int    SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private string Role     => User.FindFirstValue(ClaimTypes.Role) ?? "";

    // GET /Homework — route cho student (course list) hoặc redirect teacher
    public async Task<IActionResult> Index()
    {
        if (Role == "TEACHER" || Role == "ADMIN")
            return RedirectToAction(nameof(Teacher));

        var result = await _svc.GetStudentCoursesAsync(SchoolId, UserId);
        ViewBag.Courses = result.IsSuccess ? result.Data : new List<StudentCourseDto>();
        return View();
    }

    // ══ TEACHER ══════════════════════════════════════════════════════════════

    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Teacher()
    {
        var result = await _svc.GetTeacherAssignmentsAsync(SchoolId, UserId);
        ViewBag.Assignments = result.IsSuccess ? result.Data : new List<AssignmentListDto>();
        var saResult = await _svc.GetTeacherSubjectAssignmentsAsync(SchoolId, UserId);
        ViewBag.SubjectAssignments = saResult.IsSuccess ? saResult.Data : new List<SubjectAssignmentOptionDto>();
        return View();
    }

    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Create(int? subjectAssignmentId = null)
    {
        var saResult  = await _svc.GetTeacherSubjectAssignmentsAsync(SchoolId, UserId);
        var allSas    = saResult.IsSuccess ? saResult.Data : new List<SubjectAssignmentOptionDto>();

        // Auto-select: if teacher has a primary subject, pre-filter / pre-select
        int? primarySubjectId = await _accountSvc.GetTeacherPrimarySubjectIdAsync(SchoolId, UserId);
        int  preselectedId    = subjectAssignmentId ?? 0;

        // If no explicit pre-selection and teacher has a primary subject with exactly one SA, auto-pick it
        if (preselectedId == 0 && primarySubjectId.HasValue)
        {
            var matching = (allSas ?? []).Where(sa => sa.PrimarySubjectId == primarySubjectId.Value).ToList();
            if (matching.Count == 1)
                preselectedId = matching[0].Id;
        }

        ViewBag.SubjectAssignments  = allSas;
        ViewBag.PreselectedSaId     = preselectedId;
        ViewBag.PrimarySubjectId    = primarySubjectId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Create(
        int subjectAssignmentId, string title, string? instructions,
        AssignmentType assignmentType, decimal maxScore, DateTime? dueDate,
        bool allowLateSubmission, byte latePenaltyPercent, bool isPublished,
        IList<IFormFile>? attachments)
    {
        var req = new CreateAssignmentRequest(subjectAssignmentId, title, instructions,
            assignmentType, maxScore, dueDate, allowLateSubmission, latePenaltyPercent, isPublished);
        var result = await _svc.CreateAssignmentAsync(SchoolId, UserId, req);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Create)); }

        // result.Data là Id của bài tập vừa tạo (Result<int>). Bài tập đã được lưu thành công,
        // nên nếu một tệp đính kèm lỗi I/O ta chỉ ghi nhận tệp lỗi thay vì để controller 500.
        var failedAttachments = await SaveAttachmentsAsync(result.Data, attachments);

        TempData["Success"] = $"Đã tạo bài tập \"{title}\".";
        if (failedAttachments.Count > 0)
            TempData["Error"] = $"Một số tệp đính kèm không lưu được: {string.Join(", ", failedAttachments)}.";
        return RedirectToAction(nameof(Teacher));
    }

    // Lưu lần lượt các tệp đính kèm cho bài tập. Một tệp lỗi I/O không làm hỏng cả request:
    // ta log lại và trả về danh sách tên tệp thất bại để controller báo cho người dùng.
    private async Task<List<string>> SaveAttachmentsAsync(int assignmentId, IList<IFormFile>? attachments)
    {
        var failed = new List<string>();
        if (attachments is null) return failed;

        foreach (var f in attachments.Where(f => f.Length > 0))
        {
            try
            {
                var attResult = await _svc.AddAttachmentAsync(assignmentId, f);
                if (!attResult.IsSuccess) failed.Add(f.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu tệp đính kèm {FileName} cho bài tập {AssignmentId}", f.FileName, assignmentId);
                failed.Add(f.FileName);
            }
        }
        return failed;
    }

    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await _svc.GetAssignmentDetailAsync(SchoolId, id);
        if (!r.IsSuccess) return NotFound();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Edit(
        int id, string title, string? instructions, AssignmentType assignmentType,
        decimal maxScore, DateTime? dueDate, bool allowLateSubmission,
        byte latePenaltyPercent, bool isPublished, IList<IFormFile>? attachments)
    {
        var req = new UpdateAssignmentRequest(title, instructions, assignmentType,
            maxScore, dueDate, allowLateSubmission, latePenaltyPercent, isPublished);
        var result = await _svc.UpdateAssignmentAsync(SchoolId, id, req);
        // Update thất bại (bài tập không tồn tại / không thuộc trường này) → KHÔNG lưu đính kèm,
        // tránh tạo file + bản ghi AssignmentAttachment mồ côi trỏ tới bài tập không hợp lệ.
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Teacher)); }

        var failedAttachments = await SaveAttachmentsAsync(id, attachments);

        TempData["Success"] = "Đã cập nhật bài tập.";
        if (failedAttachments.Count > 0)
            TempData["Error"] = $"Một số tệp đính kèm không lưu được: {string.Join(", ", failedAttachments)}.";
        return RedirectToAction(nameof(Teacher));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAssignmentAsync(SchoolId, id);
        TempData[r.IsSuccess ? "Success" : "Error"] =
            r.IsSuccess ? "Đã xóa bài tập." : r.Error;
        return RedirectToAction(nameof(Teacher));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId, int assignmentId)
    {
        await _svc.DeleteAttachmentAsync(attachmentId);
        return RedirectToAction(nameof(Edit), new { id = assignmentId });
    }

    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Stats(int id)
    {
        var r = await _svc.GetStatsAsync(SchoolId, id);
        if (!r.IsSuccess) return NotFound();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Grade(int submissionId, int assignmentId, decimal score, string? feedback)
    {
        var r = await _svc.GradeSubmissionAsync(submissionId, new GradeSubmissionRequest(score, feedback));
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Đã chấm điểm." : r.Error;
        return RedirectToAction(nameof(Stats), new { id = assignmentId });
    }

    // ══ STUDENT ══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Course(int id)
    {
        var result = await _svc.GetStudentAssignmentsAsync(SchoolId, UserId, id);
        if (!result.IsSuccess) return NotFound();
        var courses = await _svc.GetStudentCoursesAsync(SchoolId, UserId);
        ViewBag.Course      = courses.Data?.FirstOrDefault(c => c.SubjectAssignmentId == id);
        ViewBag.Assignments = result.Data;
        return View();
    }

    public async Task<IActionResult> Detail(int id)
    {
        var r = await _svc.GetStudentAssignmentDetailAsync(SchoolId, UserId, id);
        if (!r.IsSuccess) return NotFound();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int assignmentId, string? answerText, IList<IFormFile>? files)
    {
        var fl = files?.Where(f => f.Length > 0) ?? Enumerable.Empty<IFormFile>();
        var r  = await _svc.SubmitAssignmentAsync(SchoolId, UserId, assignmentId, answerText, fl);
        TempData[r.IsSuccess ? "Success" : "Error"] =
            r.IsSuccess ? "Đã nộp bài thành công!" : r.Error;
        return RedirectToAction(nameof(Detail), new { id = assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(int fileId, int assignmentId)
    {
        await _svc.DeleteSubmissionFileAsync(fileId, UserId);
        return RedirectToAction(nameof(Detail), new { id = assignmentId });
    }
}
