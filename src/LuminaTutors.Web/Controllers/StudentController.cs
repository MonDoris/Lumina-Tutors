using System.Security.Claims;
using LuminaTutors.Application.DTOs.Student;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "TeacherOrAdmin")]
public sealed class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IClassService   _classService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IStudentService studentService,
        IClassService classService,
        ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _classService   = classService;
        _logger         = logger;
    }

    // ─── GET /Student ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(StudentSearchRequest? search, int page = 1)
    {
        search ??= new StudentSearchRequest(null, null, null, null, page, 20);
        search = search with { PageNumber = page };

        var result = await _studentService.SearchAsync(GetCurrentSchoolId(), search);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.SearchModel = search;
        return View(result.Data);
    }

    // ─── GET /Student/Details/5 ───────────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var result = await _studentService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Data);
    }

    // ─── GET /Student/Create (Admin only) ────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Create() => View();

    // ─── POST /Student/Create ─────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _studentService.CreateAsync(GetCurrentSchoolId(), model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Tạo học sinh thất bại.");
            return View(model);
        }

        TempData["Success"] = $"Đã thêm học sinh {result.Data!.FullName} thành công.";
        return RedirectToAction(nameof(Details), new { id = result.Data.UserId });
    }

    // ─── GET /Student/Edit/5 ──────────────────────────────────────────────────

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _studentService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
            return NotFound();

        Gender? parsedGender = Enum.TryParse<Gender>(result.Data!.Gender, out var g) ? g : null;
        var model = new UpdateStudentRequest(
            result.Data.FullName,
            result.Data.PhoneNumber,
            result.Data.DateOfBirth,
            parsedGender,
            result.Data.PlaceOfBirth,
            result.Data.PermanentAddress,
            result.Data.EthnicGroup);

        ViewBag.StudentId = id;
        return View(model);
    }

    // ─── POST /Student/Edit/5 ─────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateStudentRequest model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.StudentId = id;
            return View(model);
        }

        var result = await _studentService.UpdateAsync(GetCurrentSchoolId(), id, model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Cập nhật thất bại.");
            ViewBag.StudentId = id;
            return View(model);
        }

        TempData["Success"] = "Cập nhật thông tin học sinh thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ─── POST /Student/Deactivate/5 ───────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _studentService.DeactivateAsync(GetCurrentSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã vô hiệu hóa học sinh." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ─── GET /Student/ByClass/5 ───────────────────────────────────────────────

    public async Task<IActionResult> ByClass(int classId)
    {
        var result = await _studentService.GetByClassAsync(classId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.ClassId = classId;
        return View(result.Data);
    }

    // ─── POST /Student/Enroll ─────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int studentId, EnrollStudentRequest model)
    {
        var result = await _studentService.EnrollAsync(GetCurrentSchoolId(), studentId, model);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Xếp lớp thành công." : result.Error;

        return RedirectToAction(nameof(Details), new { id = studentId });
    }

    // ─── POST /Student/Transfer ───────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(int studentId, TransferStudentRequest model)
    {
        var result = await _studentService.TransferAsync(GetCurrentSchoolId(), studentId, model);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Chuyển lớp thành công." : result.Error;

        return RedirectToAction(nameof(Details), new { id = studentId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
