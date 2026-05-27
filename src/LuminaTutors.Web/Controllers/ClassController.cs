using System.Security.Claims;
using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class ClassController : Controller
{
    private readonly IClassService   _classService;
    private readonly ILogger<ClassController> _logger;

    public ClassController(IClassService classService, ILogger<ClassController> logger)
    {
        _classService = classService;
        _logger       = logger;
    }

    // ─── GET /Class ───────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(int academicYearId)
    {
        var result = await _classService.GetAllAsync(GetCurrentSchoolId(), academicYearId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.AcademicYearId = academicYearId;
        return View(result.Data);
    }

    // ─── GET /Class/Details/5 ─────────────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var result = await _classService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Data);
    }

    // ─── GET /Class/Create ────────────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(int academicYearId)
    {
        var teachersResult = await _classService.GetAvailableTeachersAsync(GetCurrentSchoolId());
        ViewBag.Teachers       = teachersResult.Data ?? new List<TeacherSummaryDto>();
        ViewBag.AcademicYearId = academicYearId;
        return View();
    }

    // ─── POST /Class/Create ───────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClassRequest model)
    {
        if (!ModelState.IsValid)
        {
            var teachersResult = await _classService.GetAvailableTeachersAsync(GetCurrentSchoolId());
            ViewBag.Teachers = teachersResult.Data ?? new List<TeacherSummaryDto>();
            return View(model);
        }

        var result = await _classService.CreateAsync(GetCurrentSchoolId(), model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Tạo lớp thất bại.");
            var teachersResult = await _classService.GetAvailableTeachersAsync(GetCurrentSchoolId());
            ViewBag.Teachers = teachersResult.Data ?? new List<TeacherSummaryDto>();
            return View(model);
        }

        TempData["Success"] = $"Đã tạo lớp {result.Data!.ClassName}.";
        return RedirectToAction(nameof(Details), new { id = result.Data.ClassId });
    }

    // ─── GET /Class/Edit/5 ────────────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _classService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
            return NotFound();

        var cls = result.Data!;
        var model = new UpdateClassRequest(
            cls.ClassName,
            cls.HomeRoomTeacherId,
            (byte)cls.MaxStudents,
            cls.RoomNumber);

        var teachersResult = await _classService.GetAvailableTeachersAsync(GetCurrentSchoolId());
        ViewBag.Teachers = teachersResult.Data ?? new List<TeacherSummaryDto>();
        ViewBag.ClassId  = id;
        return View(model);
    }

    // ─── POST /Class/Edit/5 ───────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateClassRequest model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ClassId = id;
            return View(model);
        }

        var result = await _classService.UpdateAsync(GetCurrentSchoolId(), id, model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Cập nhật thất bại.");
            ViewBag.ClassId = id;
            return View(model);
        }

        TempData["Success"] = "Cập nhật lớp thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ─── POST /Class/Delete ───────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int academicYearId)
    {
        var result = await _classService.DeleteAsync(GetCurrentSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa lớp học." : result.Error;

        return RedirectToAction(nameof(Index), new { academicYearId });
    }

    // ─── POST /Class/AssignSubject ────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubject(int classId, AssignSubjectRequest model)
    {
        var result = await _classService.AssignSubjectAsync(GetCurrentSchoolId(), classId, model);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Phân công môn học thành công." : result.Error;

        return RedirectToAction(nameof(Details), new { id = classId });
    }

    // ─── POST /Class/CreateSchedule ───────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(int classId, CreateScheduleRequest model)
    {
        var result = await _classService.CreateScheduleAsync(GetCurrentSchoolId(), classId, model);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Tạo lịch học thành công." : result.Error;

        return RedirectToAction(nameof(Details), new { id = classId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
