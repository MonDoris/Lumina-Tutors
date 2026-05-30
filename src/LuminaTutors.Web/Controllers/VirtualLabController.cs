using System.Security.Claims;
using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AnyAuthenticated")]
public sealed class VirtualLabController : Controller
{
    private readonly IVirtualLabService _labService;
    private readonly IAccountService   _accountService;
    private readonly ILogger<VirtualLabController> _logger;

    public VirtualLabController(
        IVirtualLabService labService,
        IAccountService accountService,
        ILogger<VirtualLabController> logger)
    {
        _labService     = labService;
        _accountService = accountService;
        _logger         = logger;
    }

    // Mapping: Subject name keywords → SubjectTag used in 3D Lab
    private static readonly (string[] Keywords, string Tag)[] SubjectTagMap =
    [
        (["hóa", "hoá", "chemistry"], "chemistry"),
        (["vật lý", "vat ly", "physics"], "physics"),
        (["sinh", "biology"], "biology"),
        (["toán", "toan", "math"], "math"),
    ];

    private static string? GuessSubjectTag(string? subjectName)
    {
        if (string.IsNullOrEmpty(subjectName)) return null;
        var lower = subjectName.ToLowerInvariant();
        foreach (var (keywords, tag) in SubjectTagMap)
            if (keywords.Any(k => lower.Contains(k)))
                return tag;
        return null;
    }

    // ─── GET /VirtualLab ──────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var result = await _labService.GetActiveSessionsAsync(GetCurrentSchoolId());
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.IsTeacher     = IsTeacher();
        ViewBag.CurrentUserId = GetCurrentUserId();

        // Auto-detect teacher's primary subject for the "Open room" form
        if (IsTeacher())
        {
            var profile = await _accountService.GetAccountByIdAsync(GetCurrentSchoolId(), GetCurrentUserId());
            if (profile.IsSuccess)
            {
                var subjectName = profile.Data!.PrimarySubjectName ?? profile.Data.SpecializationSubject;
                ViewBag.DefaultSubjectTag = GuessSubjectTag(subjectName);
            }
        }

        return View(result.Data);
    }

    // ─── POST /VirtualLab/Create ──────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLabSessionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _labService.CreateSessionAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Đã mở phòng lab. Mã tham gia: {result.Data!.SessionCode}";
        return RedirectToAction(nameof(Lab), new { id = result.Data.Id });
    }

    // ─── POST /VirtualLab/Join ────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinLabSessionRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng nhập mã phòng hợp lệ (6 ký tự).";
            return RedirectToAction(nameof(Index));
        }

        var result = await _labService.GetByCodeAsync(
            GetCurrentSchoolId(), model.SessionCode.ToUpper());

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Lab), new { id = result.Data!.Id });
    }

    // ─── GET /VirtualLab/Lab/{id} ─────────────────────────────────────────────

    public async Task<IActionResult> Lab(int id)
    {
        var result = await _labService.GetByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = "Phòng lab không tồn tại hoặc đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        if (!result.Data!.IsActive)
        {
            TempData["Error"] = "Phòng lab này đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.IsHost = result.Data.TeacherId == GetCurrentUserId();
        return View(result.Data);
    }

    // ─── POST /VirtualLab/Close/{id} ─────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _labService.CloseSessionAsync(
            GetCurrentSchoolId(), id, GetCurrentUserId());

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã kết thúc phòng lab." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private int  GetCurrentUserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int  GetCurrentSchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private bool IsTeacher()          => User.IsInRole("TEACHER") || User.IsInRole("ADMIN");
}
