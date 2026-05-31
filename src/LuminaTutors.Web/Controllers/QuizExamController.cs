using System.Security.Claims;
using LuminaTutors.Application.DTOs.Quiz;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class QuizExamController : Controller
{
    private readonly IQuizService  _quiz;
    private readonly IClassService _classService;

    public QuizExamController(IQuizService quiz, IClassService classService)
    {
        _quiz         = quiz;
        _classService = classService;
    }

    // ── GET /QuizExam ─────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        var schoolId = GetSchoolId();
        var roleCode = GetRole();

        // Teachers see only their own exams; students see all published
        int? filterByTeacher = roleCode is "TEACHER" ? GetUserId() : null;
        var statusFilter = roleCode is "STUDENT" ? "Published" : status;

        var result = await _quiz.GetExamsAsync(schoolId, filterByTeacher, statusFilter, page, 20);
        ViewBag.Role   = roleCode;
        ViewBag.Status = status;
        return View(result.IsSuccess ? result.Data : null);
    }

    // ── GET /QuizExam/Create (teacher) ────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var schoolId = GetSchoolId();
        ViewBag.Subjects    = await GetSubjectsAsync(schoolId);
        ViewBag.GradeLevels = await GetGradeLevelsAsync(schoolId);

        // Preload approved questions so the picker can filter client-side
        var qFilter = new QuizQuestionFilterRequest(ApprovedOnly: true);
        var qr      = await _quiz.GetQuestionsAsync(schoolId, qFilter, 1, 500);
        ViewBag.ApprovedQuestions = qr.IsSuccess ? qr.Data?.Items : null;

        return View();
    }

    // ── POST /QuizExam/Create ─────────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int subjectId, int? gradeLevelId, string title, string? description,
        int timeLimitMinutes, decimal pointsPerQuestion,
        bool shuffleQuestions, bool shuffleOptions, bool showResultAfter,
        DateTime? startTime, DateTime? endTime,
        [FromForm] List<int> questionIds)
    {
        var request = new CreateQuizExamRequest(
            SubjectId:         subjectId,
            GradeLevelId:      gradeLevelId,
            Title:             title,
            Description:       description,
            TimeLimitMinutes:  timeLimitMinutes,
            PointsPerQuestion: pointsPerQuestion,
            ShuffleQuestions:  shuffleQuestions,
            ShuffleOptions:    shuffleOptions,
            ShowResultAfter:   showResultAfter,
            StartTime:         startTime?.ToUniversalTime(),
            EndTime:           endTime?.ToUniversalTime(),
            QuestionIds:       questionIds);

        var result = await _quiz.CreateExamAsync(GetSchoolId(), GetUserId(), request);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = $"Đề thi \"{result.Data!.Title}\" đã được tạo.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuizExam/Detail/5 ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var schoolId = GetSchoolId();
        var result   = await _quiz.GetExamByIdAsync(schoolId, id);
        if (!result.IsSuccess) return NotFound();
        ViewBag.Role = GetRole();
        return View(result.Data);
    }

    // ── POST /QuizExam/Publish/5 ──────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var result = await _quiz.PublishExamAsync(GetSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đề thi đã được mở cho học sinh." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuizExam/Close/5 ────────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _quiz.CloseExamAsync(GetSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đề thi đã đóng." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuizExam/Delete/5 ───────────────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _quiz.DeleteExamAsync(GetSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa đề thi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuizExam/Results/5 (teacher) ────────────────────────────────────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpGet]
    public async Task<IActionResult> Results(int id, int page = 1)
    {
        var schoolId   = GetSchoolId();
        var examResult = await _quiz.GetExamByIdAsync(schoolId, id);
        if (!examResult.IsSuccess) return NotFound();

        var results = await _quiz.GetExamResultsAsync(schoolId, id, page, 50);
        ViewBag.Exam = examResult.Data;
        return View(results.IsSuccess ? results.Data : null);
    }

    // ── GET /QuizExam/Take/5 (student) ───────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Take(int id)
    {
        var result = await _quiz.StartAttemptAsync(GetSchoolId(), GetUserId(), id);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        // Already submitted → redirect to result
        if (result.Data!.Status == "Submitted")
            return RedirectToAction(nameof(Result), new { id = result.Data.AttemptId });

        return View(result.Data);
    }

    // ── POST /QuizExam/SaveAnswer ─────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(
        int attemptId, int questionId, int? selectedOptionId)
    {
        var request = new SaveAnswerRequest(questionId, selectedOptionId);
        var result  = await _quiz.SaveAnswerAsync(attemptId, GetUserId(), request);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── POST /QuizExam/Submit/5 ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int attemptId)
    {
        var result = await _quiz.SubmitAttemptAsync(attemptId, GetUserId());

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Đã nộp bài. Điểm của bạn: {result.Data!.Score:F1}/{result.Data.MaxScore:F0}";
        return RedirectToAction(nameof(Result), new { id = attemptId });
    }

    // ── GET /QuizExam/Result/5 ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var result = await _quiz.GetAttemptResultAsync(id, GetUserId());
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    // ── GET /QuizExam/StudentResult/5 (teacher views a specific attempt) ─────
    [Authorize(Policy = "TeacherOrAdmin")]
    [HttpGet]
    public async Task<IActionResult> StudentResult(int id)
    {
        var result = await _quiz.GetAttemptResultAsync(id, GetUserId());
        if (!result.IsSuccess) return NotFound();
        return View("Result", result.Data);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int    GetUserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int    GetSchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private string GetRole()     => User.FindFirstValue(ClaimTypes.Role) ?? "";

    private async Task<object> GetSubjectsAsync(int schoolId)
    {
        var r = await _classService.GetSubjectsAsync(schoolId);
        return r.IsSuccess ? r.Data! : new List<object>();
    }
    private async Task<object> GetGradeLevelsAsync(int schoolId)
    {
        var r = await _classService.GetGradeLevelsAsync(schoolId);
        return r.IsSuccess ? r.Data! : new List<object>();
    }
}
