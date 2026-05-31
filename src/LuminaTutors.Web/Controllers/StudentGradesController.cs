using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Học sinh xem điểm của chính mình. Policy AnyAuthenticated — student không thể
/// xem điểm của người khác vì studentId luôn lấy từ claims.
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class StudentGradesController : Controller
{
    private readonly IGradingService _grading;
    private readonly IClassService   _classService;

    public StudentGradesController(IGradingService grading, IClassService classService)
    {
        _grading      = grading;
        _classService = classService;
    }

    public async Task<IActionResult> Index(int? semesterId)
    {
        var schoolId  = SchoolId();
        var studentId = UserId();

        // Lấy danh sách học kỳ để học sinh chọn
        var semResult = await _classService.GetSemestersAsync(schoolId);
        var semesters = semResult.IsSuccess ? semResult.Data! : new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();

        ViewBag.Semesters   = semesters;
        ViewBag.SemesterId  = semesterId;

        if (semesterId is null)
            return View(null);

        var result = await _grading.GetStudentSemesterSummaryAsync(studentId, semesterId.Value);
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(null);
        }

        return View(result.Data);
    }

    private int SchoolId() => int.Parse(User.FindFirstValue("SchoolId")                ?? "0");
    private int UserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
