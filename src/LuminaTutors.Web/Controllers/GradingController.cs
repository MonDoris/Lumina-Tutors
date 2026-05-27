using System.Security.Claims;
using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class GradingController : Controller
{
    private readonly IGradingService _gradingService;
    private readonly ILogger<GradingController> _logger;

    public GradingController(IGradingService gradingService, ILogger<GradingController> logger)
    {
        _gradingService = gradingService;
        _logger         = logger;
    }

    // ─── GET /Grading ─────────────────────────────────────────────────────────

    public IActionResult Index() => View();

    // ─── GET /Grading/GradeBook/5 ─────────────────────────────────────────────

    public async Task<IActionResult> GradeBook(int subjectAssignmentId)
    {
        var result = await _gradingService.GetSubjectGradeBookAsync(subjectAssignmentId);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Data);
    }

    // ─── GET /Grading/StudentSummary ──────────────────────────────────────────

    public async Task<IActionResult> StudentSummary(int studentId, int semesterId)
    {
        var result = await _gradingService.GetStudentSemesterSummaryAsync(studentId, semesterId);
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── POST /Grading/EnterScore ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterScore(EnterScoreRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu điểm không hợp lệ.";
            return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId = model.SubjectAssignmentId });
        }

        var result = await _gradingService.EnterScoreAsync(
            GetCurrentSchoolId(), GetCurrentTeacherId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId = model.SubjectAssignmentId });
        }

        // AJAX support
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(result.Data);

        TempData["Success"] = "Nhập điểm thành công.";
        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId = model.SubjectAssignmentId });
    }

    // ─── POST /Grading/BulkEnterScore ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkEnterScore(BulkEnterScoreRequest model)
    {
        var result = await _gradingService.BulkEnterScoresAsync(
            GetCurrentSchoolId(), GetCurrentTeacherId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã nhập {result.Data!.Count} điểm thành công."
                : result.Error;

        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId = model.SubjectAssignmentId });
    }

    // ─── POST /Grading/DeleteScore ────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScore(int scoreEntryId, int subjectAssignmentId)
    {
        var result = await _gradingService.DeleteScoreAsync(scoreEntryId, GetCurrentTeacherId());

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess ? Ok() : BadRequest(result.Error);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa điểm." : result.Error;

        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId });
    }

    // ─── POST /Grading/CalculateAverage ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateAverage(int studentId, int subjectAssignmentId)
    {
        var result = await _gradingService.CalculateAverageAsync(studentId, subjectAssignmentId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"ĐTBm = {result.Data!.AverageScore:F1}"
                : result.Error;

        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId });
    }

    // ─── POST /Grading/CalculateAllAverages ───────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateAllAverages(int subjectAssignmentId)
    {
        var result = await _gradingService.CalculateAllAveragesAsync(subjectAssignmentId);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã tính điểm cho {result.Data} học sinh."
                : result.Error;

        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId });
    }

    // ─── POST /Grading/LockGradeBook ──────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockGradeBook(int subjectAssignmentId)
    {
        var result = await _gradingService.LockGradeBookAsync(subjectAssignmentId, GetCurrentUserId());

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Sổ điểm đã được khóa." : result.Error;

        return RedirectToAction(nameof(GradeBook), new { subjectAssignmentId });
    }

    // ─── GET /Grading/Exams ───────────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Exams(int semesterId)
    {
        var result = await _gradingService.GetExamsAsync(GetCurrentSchoolId(), semesterId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.SemesterId = semesterId;
        return View(result.Data);
    }

    // ─── POST /Grading/CreateExam ─────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExam(CreateExamRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu kỳ thi không hợp lệ.";
            return RedirectToAction(nameof(Exams), new { semesterId = model.SemesterId });
        }

        var result = await _gradingService.CreateExamAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã tạo kỳ thi: {result.Data!.ExamName}"
                : result.Error;

        return RedirectToAction(nameof(Exams), new { semesterId = model.SemesterId });
    }

    // ─── GET /Grading/ExamRooms/5 ─────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExamRooms(int examId)
    {
        var result = await _gradingService.GetExamRoomsAsync(examId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.ExamId = examId;
        return View(result.Data);
    }

    // ─── POST /Grading/AssignSeats ────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSeats(int examId)
    {
        var result = await _gradingService.AssignSeatsRandomAsync(examId);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã xếp phòng thi ngẫu nhiên cho {result.Data} thí sinh."
                : result.Error;

        return RedirectToAction(nameof(ExamRooms), new { examId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    /// <summary>
    /// Teacher profile ID (stored separately from User ID).
    /// In production: lookup TeacherProfile.Id from DB; here we use UserId as proxy
    /// until a proper claim is added.
    /// </summary>
    private int GetCurrentTeacherId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
