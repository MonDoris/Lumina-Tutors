using System.Security.Claims;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class SubjectController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SubjectController> _logger;

    public SubjectController(IUnitOfWork uow, ILogger<SubjectController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    // ── GET /Subject ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        var all = await _uow.Subjects.FindAsync(
            s => s.SchoolId == SchoolId);

        if (!string.IsNullOrWhiteSpace(search))
            all = all.Where(s =>
                s.SubjectName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.SubjectCode.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        if (activeOnly == true)
            all = all.Where(s => s.IsActive).ToList();

        ViewBag.Search     = search;
        ViewBag.ActiveOnly = activeOnly ?? false;
        return View(all.OrderBy(s => s.SubjectName).ToList());
    }

    // ── GET /Subject/Create ───────────────────────────────────────────────────

    public IActionResult Create() => View();

    // ── POST /Subject/Create ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string subjectCode, string subjectName,
        SubjectCategory subjectCategory, bool has3DLab)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ModelState.AddModelError("", "Tên môn học không được để trống.");
            return View();
        }

        // Kiểm tra mã môn trùng
        var existing = await _uow.Subjects.FindAsync(
            s => s.SchoolId == SchoolId && s.SubjectCode == subjectCode.Trim().ToUpper());
        if (existing.Any())
        {
            ModelState.AddModelError("", $"Mã môn học '{subjectCode}' đã tồn tại.");
            return View();
        }

        var subject = new Subject
        {
            SchoolId         = SchoolId,
            SubjectCode      = subjectCode.Trim().ToUpper(),
            SubjectName      = subjectName.Trim(),
            SubjectCategory  = subjectCategory,
            Has3DLab         = has3DLab,
            IsActive         = true
        };

        await _uow.Subjects.AddAsync(subject);
        await _uow.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo môn học \"{subject.SubjectName}\" (mã: {subject.SubjectCode}).";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Subject/Edit/5 ───────────────────────────────────────────────────

    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _uow.Subjects.GetByIdAsync(id);
        if (subject == null || subject.SchoolId != SchoolId) return NotFound();
        return View(subject);
    }

    // ── POST /Subject/Edit/5 ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, string subjectCode, string subjectName,
        SubjectCategory subjectCategory, bool has3DLab, bool isActive)
    {
        var subject = await _uow.Subjects.GetByIdAsync(id);
        if (subject == null || subject.SchoolId != SchoolId) return NotFound();

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ModelState.AddModelError("", "Tên môn học không được để trống.");
            return View(subject);
        }

        // Kiểm tra mã trùng với môn khác
        var dup = await _uow.Subjects.FindAsync(
            s => s.SchoolId == SchoolId &&
                 s.SubjectCode == subjectCode.Trim().ToUpper() &&
                 s.Id != id);
        if (dup.Any())
        {
            ModelState.AddModelError("", $"Mã môn học '{subjectCode}' đã được dùng bởi môn khác.");
            return View(subject);
        }

        subject.SubjectCode     = subjectCode.Trim().ToUpper();
        subject.SubjectName     = subjectName.Trim();
        subject.SubjectCategory = subjectCategory;
        subject.Has3DLab        = has3DLab;
        subject.IsActive        = isActive;

        await _uow.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật môn học \"{subject.SubjectName}\".";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subject/Toggle/5 (bật/tắt IsActive) ────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var subject = await _uow.Subjects.GetByIdAsync(id);
        if (subject == null || subject.SchoolId != SchoolId) return NotFound();

        subject.IsActive = !subject.IsActive;
        await _uow.SaveChangesAsync();

        TempData["Success"] = subject.IsActive
            ? $"Đã kích hoạt môn học \"{subject.SubjectName}\"."
            : $"Đã vô hiệu hóa môn học \"{subject.SubjectName}\".";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subject/Delete/5 ────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _uow.Subjects.GetByIdAsync(id);
        if (subject == null || subject.SchoolId != SchoolId) return NotFound();

        try
        {
            _uow.Subjects.Remove(subject);
            await _uow.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa môn học \"{subject.SubjectName}\".";
        }
        catch
        {
            TempData["Error"] = "Không thể xóa môn học này vì đang được sử dụng trong hệ thống. Hãy vô hiệu hóa thay vì xóa.";
        }

        return RedirectToAction(nameof(Index));
    }
}
