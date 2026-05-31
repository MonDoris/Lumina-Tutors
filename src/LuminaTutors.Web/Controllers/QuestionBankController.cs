using System.Security.Claims;
using LuminaTutors.Application.DTOs.QuestionBank;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class QuestionBankController : Controller
{
    private readonly IQuestionBankService _service;
    private readonly ILogger<QuestionBankController> _logger;

    public QuestionBankController(
        IQuestionBankService service,
        ILogger<QuestionBankController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    // ── GET /QuestionBank ─────────────────────────────────────────────────────

    public async Task<IActionResult> Index([FromQuery] QuestionFilterRequest filter)
    {
        var result = await _service.GetQuestionsAsync(SchoolId, filter);
        var stats  = await _service.GetStatsAsync(SchoolId);

        ViewBag.Paged  = result.IsSuccess  ? result.Data  : null;
        ViewBag.Stats  = stats.IsSuccess   ? stats.Data   : null;
        ViewBag.Filter = filter;
        return View();
    }

    // ── GET /QuestionBank/Details/5 ───────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var result = await _service.GetByIdAsync(SchoolId, id);
        if (!result.IsSuccess) return NotFound();
        return View(result.Data);
    }

    // ── GET /QuestionBank/Create ──────────────────────────────────────────────

    public IActionResult Create() => View();

    // ── POST /QuestionBank/Create ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionRequest req)
    {
        if (!ModelState.IsValid) return View(req);

        var result = await _service.CreateAsync(SchoolId, UserId, req);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error!);
            return View(req);
        }

        TempData["Success"] = "Đã thêm câu hỏi vào ngân hàng.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuestionBank/Edit/5 ──────────────────────────────────────────────

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _service.GetByIdAsync(SchoolId, id);
        if (!result.IsSuccess) return NotFound();
        return View(result.Data);
    }

    // ── POST /QuestionBank/Edit/5 ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateQuestionRequest req)
    {
        if (!ModelState.IsValid) return View(req);

        var result = await _service.UpdateAsync(SchoolId, id, req);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error!);
            return View(req);
        }

        TempData["Success"] = "Đã cập nhật câu hỏi.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuestionBank/Delete/5 ───────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(SchoolId, id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa câu hỏi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuestionBank/Approve/5 ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _service.ApproveAsync(SchoolId, id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Câu hỏi đã được duyệt." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuestionBank/Import ──────────────────────────────────────────────

    public async Task<IActionResult> Import()
    {
        var jobs     = await _service.GetImportJobsAsync(SchoolId);
        var subjects = await _service.GetSubjectsAsync();
        ViewBag.Jobs     = jobs.IsSuccess     ? jobs.Data!.ToList()     : new List<ImportJobDto>();
        ViewBag.Subjects = subjects.IsSuccess ? subjects.Data!.ToList() : new List<SubjectOptionDto>();
        return View();
    }

    // ── POST /QuestionBank/PreviewUrl ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewUrl(ImportFromUrlRequest req)
    {
        var result = await _service.PreviewUrlImportAsync(SchoolId, UserId, req);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // ── POST /QuestionBank/ImportFromUrl ─────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportFromUrl(ImportFromUrlRequest req)
    {
        var result = await _service.ExecuteUrlImportAsync(SchoolId, UserId, req);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Import));
        }

        TempData["Success"] = $"Đã import {result.Data!.ImportedCount} câu hỏi từ URL.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuestionBank/ImportExcel ────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(int subjectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file Excel (.xlsx).";
            return RedirectToAction(nameof(Import));
        }

        var result = await _service.ImportFromExcelAsync(SchoolId, UserId, subjectId, file);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Đã import {result.Data} câu hỏi từ Excel." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuestionBank/ImportWord ─────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportWord(int subjectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file Word (.docx).";
            return RedirectToAction(nameof(Import));
        }

        var result = await _service.ImportFromWordAsync(SchoolId, UserId, subjectId, file);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Đã import {result.Data} câu hỏi từ Word." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ── POST /QuestionBank/ImportPdf ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPdf(int subjectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file PDF.";
            return RedirectToAction(nameof(Import));
        }

        var result = await _service.ImportFromPdfAsync(SchoolId, UserId, subjectId, file);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Đã import {result.Data} câu hỏi từ PDF." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ── GET /QuestionBank/Stats (JSON) ────────────────────────────────────────

    public async Task<IActionResult> Stats()
    {
        var result = await _service.GetStatsAsync(SchoolId);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Json(result.Data);
    }
}
