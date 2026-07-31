using System.Security.Claims;
using LuminaTutors.Application.DTOs.Course;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// E-Learning — phía học sinh: khóa học của tôi, mục lục, học bài.
/// Heartbeat video POST mỗi 15s (fetch keepalive + antiforgery header).
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class LearnController : Controller
{
    private readonly ICourseService _svc;
    private readonly ILogger<LearnController> _logger;

    public LearnController(ICourseService svc, ILogger<LearnController> logger)
    {
        _svc    = svc;
        _logger = logger;
    }

    private int UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    // GET /Learn — khóa học của tôi
    public async Task<IActionResult> Index()
    {
        var result = await _svc.GetMyCoursesAsync(SchoolId, UserId);
        ViewBag.Courses = result.IsSuccess ? result.Data : new List<MyCourseDto>();
        return View();
    }

    // GET /Learn/Course/5 — mục lục khóa học
    public async Task<IActionResult> Course(int id)
    {
        var result = await _svc.GetCourseOutlineAsync(SchoolId, id, UserId);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }
        ViewBag.Outline = result.Data;
        return View();
    }

    // GET /Learn/Lesson/5 — học bài (tạo LessonProgress lazy + resume point)
    public async Task<IActionResult> Lesson(int id)
    {
        var result = await _svc.OpenLessonAsync(SchoolId, id, UserId);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }
        ViewBag.Lesson = result.Data;
        return View();
    }

    // POST /Learn/Heartbeat/5 — cập nhật tiến độ xem video
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Heartbeat(int id, [FromBody] VideoHeartbeatRequest request)
    {
        var result = await _svc.UpdateVideoProgressAsync(SchoolId, id, UserId, request);
        return result.IsSuccess ? Json(new { ok = true, data = result.Data })
                                : Json(new { ok = false, error = result.Error });
    }

    // POST /Learn/Complete/5 — đánh dấu hoàn thành bài học
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _svc.CompleteLessonAsync(SchoolId, id, UserId);
        return result.IsSuccess ? Json(new { ok = true, data = result.Data })
                                : Json(new { ok = false, error = result.Error });
    }
}
