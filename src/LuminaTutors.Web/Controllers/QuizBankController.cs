using System.Security.Claims;
using LuminaTutors.Application.DTOs.Quiz;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class QuizBankController : Controller
{
    private readonly IQuizService  _quiz;
    private readonly IClassService _classService;

    public QuizBankController(IQuizService quiz, IClassService classService)
    {
        _quiz         = quiz;
        _classService = classService;
    }

    // ── GET /QuizBank ─────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(
        int? subjectId, int? gradeLevelId, string? difficulty,
        string? keyword, bool? approvedOnly,
        int page = 1)
    {
        var schoolId = GetSchoolId();
        var filter   = new QuizQuestionFilterRequest(
            SubjectId:    subjectId,
            GradeLevelId: gradeLevelId,
            Difficulty:   difficulty,
            Keyword:      keyword,
            ApprovedOnly: approvedOnly);

        var result = await _quiz.GetQuestionsAsync(schoolId, filter, page, pageSize: 20);

        ViewBag.Filter       = filter;
        ViewBag.Subjects     = await GetSubjectsAsync(schoolId);
        ViewBag.GradeLevels  = await GetGradeLevelsAsync(schoolId);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(result.Data);
        }
        return View(result.Data);
    }

    // ── GET /QuizBank/Create ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var schoolId = GetSchoolId();
        ViewBag.Subjects    = await GetSubjectsAsync(schoolId);
        ViewBag.GradeLevels = await GetGradeLevelsAsync(schoolId);
        return View();
    }

    // ── POST /QuizBank/Create ─────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int subjectId, string questionText, string questionType,
        string difficultyLevel, int? gradeLevelId, string? chapterTag,
        string? explanationText,
        [FromForm] List<string> optionText,
        [FromForm] List<string> optionLabel,
        [FromForm] int correctIndex)
    {
        var schoolId  = GetSchoolId();
        var teacherId = GetUserId();

        var options = optionLabel
            .Zip(optionText, (label, text) => (label, text))
            .Select((pair, idx) => new CreateOptionRequest(
                OptionLabel: pair.label.Length > 0 ? pair.label[0] : (char)('A' + idx),
                OptionText:  pair.text,
                IsCorrect:   idx == correctIndex))
            .ToList();

        var request = new CreateQuestionRequest(
            SubjectId:      subjectId,
            QuestionText:   questionText,
            QuestionType:   questionType,
            DifficultyLevel:difficultyLevel,
            GradeLevelId:   gradeLevelId,
            ChapterTag:     chapterTag,
            ExplanationText:explanationText,
            Options:        options);

        var result = await _quiz.CreateQuestionAsync(schoolId, teacherId, request);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = "Đã thêm câu hỏi vào ngân hàng.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuizBank/Edit/5 ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var schoolId = GetSchoolId();
        var result   = await _quiz.GetQuestionByIdAsync(schoolId, id);
        if (!result.IsSuccess) return NotFound();

        ViewBag.Subjects    = await GetSubjectsAsync(schoolId);
        ViewBag.GradeLevels = await GetGradeLevelsAsync(schoolId);
        return View(result.Data);
    }

    // ── POST /QuizBank/Edit/5 ─────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, string questionText, string difficultyLevel,
        string? chapterTag, string? explanationText,
        [FromForm] List<string> optionText,
        [FromForm] List<string> optionLabel,
        [FromForm] int correctIndex)
    {
        var schoolId = GetSchoolId();

        var options = optionLabel
            .Zip(optionText, (label, text) => (label, text))
            .Select((pair, idx) => new CreateOptionRequest(
                OptionLabel: pair.label.Length > 0 ? pair.label[0] : (char)('A' + idx),
                OptionText:  pair.text,
                IsCorrect:   idx == correctIndex))
            .ToList();

        var request = new UpdateQuestionRequest(
            QuestionText:   questionText,
            DifficultyLevel:difficultyLevel,
            ChapterTag:     chapterTag,
            ExplanationText:explanationText,
            Options:        options);

        var result = await _quiz.UpdateQuestionAsync(schoolId, id, request);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = "Đã cập nhật câu hỏi.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuizBank/Delete/5 ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _quiz.DeleteQuestionAsync(GetSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa câu hỏi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuizBank/Approve/5 ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _quiz.ApproveQuestionAsync(GetSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã duyệt câu hỏi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int GetUserId()  => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int GetSchoolId()=> int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    private async Task<object> GetSubjectsAsync(int schoolId)
    {
        var result = await _classService.GetSubjectsAsync(schoolId);
        return result.IsSuccess ? result.Data! : new List<object>();
    }

    private async Task<object> GetGradeLevelsAsync(int schoolId)
    {
        var result = await _classService.GetGradeLevelsAsync(schoolId);
        return result.IsSuccess ? result.Data! : new List<object>();
    }
}
