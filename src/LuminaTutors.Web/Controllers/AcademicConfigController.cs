using System.Security.Claims;
using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class AcademicConfigController : Controller
{
    private readonly IClassService _classService;

    public AcademicConfigController(IClassService classService)
    {
        _classService = classService;
    }

    public async Task<IActionResult> Index()
    {
        var sid = GetCurrentSchoolId();

        var years = await _classService.GetAcademicYearsForConfigAsync(sid);
        var levels = await _classService.GetGradeLevelsForConfigAsync(sid);

        var vm = new AcademicConfigViewModel
        {
            AcademicYears = years.Data?.ToList() ?? [],
            GradeLevels = levels.Data?.ToList() ?? [],
            NewAcademicYear = new CreateAcademicYearRequest(
                YearName: $"{DateTime.Today.Year}-{DateTime.Today.Year + 1}",
                StartDate: DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 9, 1)),
                EndDate: DateOnly.FromDateTime(new DateTime(DateTime.Today.Year + 1, 5, 31)),
                IsActive: false),
            NewGradeLevel = new CreateGradeLevelRequest(1, "Khối 1", "TieuHoc")
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicYear(CreateAcademicYearRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin năm học.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _classService.CreateAcademicYearAsync(GetCurrentSchoolId(), request);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã tạo năm học." : (result.Error ?? "Tạo năm học thất bại.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGradeLevel(CreateGradeLevelRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin khối lớp.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _classService.CreateGradeLevelAsync(GetCurrentSchoolId(), request);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã tạo khối lớp." : (result.Error ?? "Tạo khối lớp thất bại.");

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}

