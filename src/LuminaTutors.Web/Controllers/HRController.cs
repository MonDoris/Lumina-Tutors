using System.Security.Claims;
using LuminaTutors.Application.DTOs.HR;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class HRController : Controller
{
    private readonly IHRService            _hrService;
    private readonly ILogger<HRController> _logger;

    public HRController(IHRService hrService, ILogger<HRController> logger)
    {
        _hrService = hrService;
        _logger    = logger;
    }

    // GET /HR/Teachers
    public async Task<IActionResult> Teachers(string? keyword, int page = 1)
    {
        var result = await _hrService.GetTeachersAsync(
            GetCurrentSchoolId(), keyword, page, pageSize: 20);

        ViewBag.Keyword = keyword;
        ViewBag.Page    = page;
        return View(result.IsSuccess ? result.Data : null);
    }

    // GET /HR/TeacherDetail/5
    public async Task<IActionResult> TeacherDetail(int id)
    {
        var result = await _hrService.GetTeacherByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess) return NotFound();
        return View(result.Data);
    }

    // GET /HR/CreateTeacher
    public IActionResult CreateTeacher() => View(new CreateTeacherRequest(
        FullName: "", Email: "", TeacherCode: "", PhoneNumber: null,
        DateOfBirth: null, Gender: null, Qualification: null,
        SpecializationSubject: null, HireDate: null));

    // POST /HR/CreateTeacher
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher(CreateTeacherRequest model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _hrService.CreateTeacherAsync(GetCurrentSchoolId(), model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error ?? "Có lỗi xảy ra.");
            return View(model);
        }

        TempData["Success"] = $"Đã tạo giáo viên {result.Data!.FullName} thành công.";
        return RedirectToAction(nameof(Teachers));
    }

    // GET /HR/EditTeacher/5
    public async Task<IActionResult> EditTeacher(int id)
    {
        var result = await _hrService.GetTeacherByIdAsync(GetCurrentSchoolId(), id);
        if (!result.IsSuccess) return NotFound();

        var t = result.Data!;
        var model = new UpdateTeacherRequest(
            FullName:              t.FullName,
            TeacherCode:           t.TeacherCode,
            PhoneNumber:           t.PhoneNumber,
            DateOfBirth:           t.DateOfBirth,
            Gender:                Enum.TryParse<Gender>(t.Gender, out var g) ? g : null,
            Qualification:         t.Qualification,
            SpecializationSubject: t.SpecializationSubject,
            HireDate:              t.HireDate,
            ContractType:          Enum.TryParse<ContractType>(t.ContractType, out var ctype) ? ctype : ContractType.FullTime,
            BankName:              t.BankName);

        ViewBag.TeacherId = id;
        ViewBag.Email     = t.Email;
        return View(model);
    }

    // POST /HR/EditTeacher/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeacher(int id, UpdateTeacherRequest model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TeacherId = id;
            return View(model);
        }

        var result = await _hrService.UpdateTeacherAsync(GetCurrentSchoolId(), id, model);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Error ?? "Có lỗi xảy ra.");
            ViewBag.TeacherId = id;
            return View(model);
        }

        TempData["Success"] = $"Đã cập nhật giáo viên {result.Data!.FullName}.";
        return RedirectToAction(nameof(TeacherDetail), new { id });
    }

    // POST /HR/DeactivateTeacher
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateTeacher(int id)
    {
        var result = await _hrService.DeactivateTeacherAsync(GetCurrentSchoolId(), id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã vô hiệu hóa giáo viên." : result.Error;
        return RedirectToAction(nameof(Teachers));
    }

    // GET /HR/Payroll
    public async Task<IActionResult> Payroll(byte? month, short? year)
    {
        month ??= (byte)DateTime.Now.Month;
        year  ??= (short)DateTime.Now.Year;

        var result = await _hrService.GetPayrollsAsync(GetCurrentSchoolId(), month.Value, year.Value);

        ViewBag.Month = month;
        ViewBag.Year  = year;
        return View(result.IsSuccess ? result.Data : null);
    }

    // POST /HR/CreatePayroll
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayroll(CreatePayrollRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Payroll));
        }

        var result = await _hrService.CreatePayrollAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã tạo bảng lương thành công." : result.Error;

        return RedirectToAction(nameof(Payroll));
    }

    // POST /HR/ApprovePayroll
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayroll(int id)
    {
        var result = await _hrService.ApprovePayrollAsync(id, GetCurrentUserId());
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã duyệt bảng lương." : result.Error;
        return RedirectToAction(nameof(Payroll));
    }

    // POST /HR/CreateContract
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContract(CreateContractRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(TeacherDetail), new { id = model.TeacherId });
        }

        var result = await _hrService.CreateContractAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã tạo hợp đồng thành công." : result.Error;

        return RedirectToAction(nameof(TeacherDetail), new { id = model.TeacherId });
    }

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");

    private int GetCurrentUserId() =>
        // Lúc đăng nhập chỉ set ClaimTypes.NameIdentifier (không có claim "UserId"),
        // nên phải đọc đúng claim này — nếu không sẽ trả 0 và gây vi phạm FK khi lưu ApprovedBy/CreatedBy.
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
