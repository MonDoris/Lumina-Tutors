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

    public async Task<IActionResult> Index(int? academicYearId)
    {
        var sid = GetCurrentSchoolId();

        // Load academic years for the dropdown
        var ayResult = await _classService.GetAcademicYearsAsync(sid);
        var years    = ayResult.Data?.ToList() ?? new List<AcademicYearSelectDto>();
        ViewBag.AcademicYears = years;

        // Default to the active year if none selected
        var selectedYearId = academicYearId
            ?? years.FirstOrDefault(y => y.IsActive)?.AcademicYearId
            ?? years.FirstOrDefault()?.AcademicYearId
            ?? 0;

        ViewBag.AcademicYearId = selectedYearId;

        if (selectedYearId == 0)
            return View(new List<ClassSummaryDto>());

        var result = await _classService.GetAllAsync(sid, selectedYearId);
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data ?? new List<ClassSummaryDto>());
    }

    // ─── GET /Class/Details/5 ─────────────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var sid    = GetCurrentSchoolId();
        var result = await _classService.GetByIdAsync(sid, id);
        if (!result.IsSuccess)
            return NotFound();

        // Load unassigned students for the Admin enroll modal
        if (User.IsInRole("ADMIN"))
        {
            var unassigned = await _classService.GetUnassignedStudentsAsync(sid, id);
            ViewBag.UnassignedStudents = unassigned.Data?.ToList() ?? new List<ClassStudentDto>();
        }

        return View(result.Data);
    }

    // ─── GET /Class/Create ────────────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(int? academicYearId)
    {
        var sid = GetCurrentSchoolId();
        var teachersResult     = await _classService.GetAvailableTeachersAsync(sid);
        var gradeLevelsResult  = await _classService.GetGradeLevelsAsync(sid);
        var academicYearsResult= await _classService.GetAcademicYearsAsync(sid);

        var teachers    = teachersResult.Data?.ToList()      ?? new List<TeacherSummaryDto>();
        var gradeLevels = gradeLevelsResult.Data?.ToList()   ?? new List<GradeLevelSelectDto>();
        var years       = academicYearsResult.Data?.ToList() ?? new List<AcademicYearSelectDto>();

        ViewBag.Teachers      = teachers;
        ViewBag.GradeLevels   = gradeLevels;
        ViewBag.AcademicYears = years;

        // Auto-pick defaults so users don't have to select manually
        var defaultYearId =
            academicYearId
            ?? years.FirstOrDefault(y => y.IsActive)?.AcademicYearId
            ?? years.FirstOrDefault()?.AcademicYearId
            ?? 0;

        var defaultGradeLevelId =
            gradeLevels.FirstOrDefault()?.GradeLevelId
            ?? 0;

        var model = new CreateClassRequest(
            ClassName: "",
            GradeLevelId: defaultGradeLevelId,
            AcademicYearId: defaultYearId,
            HomeRoomTeacherId: null,
            MaxStudents: 40,
            RoomNumber: null);

        return View(model);
    }

    // ─── POST /Class/Create ───────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClassRequest model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateSelectListsAsync();
            return View(model);
        }

        var result = await _classService.CreateAsync(GetCurrentSchoolId(), model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Tạo lớp thất bại.");
            await LoadCreateSelectListsAsync();
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

    // ─── POST /Class/EnrollStudent ────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(int classId, int studentUserId)
    {
        var result = await _classService.EnrollStudentAsync(GetCurrentSchoolId(), classId, studentUserId);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã thêm học sinh vào lớp." : result.Error;
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    // ─── POST /Class/RemoveStudent ────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudent(int classId, int studentUserId)
    {
        var result = await _classService.RemoveStudentAsync(GetCurrentSchoolId(), classId, studentUserId);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã xóa học sinh khỏi lớp." : result.Error;
        return RedirectToAction(nameof(Details), new { id = classId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    private async Task LoadCreateSelectListsAsync()
    {
        var sid = GetCurrentSchoolId();
        var teachers      = await _classService.GetAvailableTeachersAsync(sid);
        var gradeLevels   = await _classService.GetGradeLevelsAsync(sid);
        var academicYears = await _classService.GetAcademicYearsAsync(sid);
        ViewBag.Teachers      = teachers.Data?.ToList()      ?? new List<TeacherSummaryDto>();
        ViewBag.GradeLevels   = gradeLevels.Data?.ToList()   ?? new List<GradeLevelSelectDto>();
        ViewBag.AcademicYears = academicYears.Data?.ToList() ?? new List<AcademicYearSelectDto>();
    }
}
