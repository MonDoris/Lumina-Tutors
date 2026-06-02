using System.Security.Claims;
using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "FinanceAccess")]
public sealed class FinanceController : Controller
{
    private readonly IFinanceService   _financeService;
    private readonly IAccountService   _accountService;
    private readonly IClassService     _classService;
    private readonly ILogger<FinanceController> _logger;

    public FinanceController(
        IFinanceService  financeService,
        IAccountService  accountService,
        IClassService    classService,
        ILogger<FinanceController> logger)
    {
        _financeService = financeService;
        _accountService = accountService;
        _classService   = classService;
        _logger         = logger;
    }

    // ─── GET /Finance ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(int? month, int? year)
    {
        month ??= DateTime.Now.Month;
        year  ??= DateTime.Now.Year;

        var report = await _financeService.GetMonthlyReportAsync(GetCurrentSchoolId(), month.Value, year.Value);

        ViewBag.Month = month;
        ViewBag.Year  = year;
        return View(report.IsSuccess ? report.Data : null);
    }

    // ─── GET /Finance/Invoices ────────────────────────────────────────────────

    public async Task<IActionResult> Invoices(
        string? status, int? studentId, string? billingPeriod, int page = 1)
    {
        var result = await _financeService.GetInvoicesAsync(
            GetCurrentSchoolId(), status, studentId, billingPeriod, page, pageSize: 20);

        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.Status        = status;
        ViewBag.StudentId     = studentId;
        ViewBag.BillingPeriod = billingPeriod;
        return View(result.Data);
    }

    // ─── GET /Finance/InvoiceDetails/5 ───────────────────────────────────────

    public async Task<IActionResult> InvoiceDetails(int id)
    {
        var result = await _financeService.GetInvoiceAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Data);
    }

    // ─── GET /Finance/OutstandingDebts ────────────────────────────────────────

    public async Task<IActionResult> OutstandingDebts()
    {
        var result = await _financeService.GetOutstandingDebtsAsync(GetCurrentSchoolId());
        if (!result.IsSuccess)
            return StatusCode(500);

        return View(result.Data);
    }

    // ─── GET /Finance/CreateInvoice ──────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateInvoice()
    {
        await LoadCreateInvoiceSelectListsAsync();
        return View(new CreateInvoiceRequest(0, 0, DateTime.Now.ToString("yyyy-MM"), 0, 0,
            DateOnly.FromDateTime(DateTime.Today.AddDays(15))));
    }

    // ─── POST /Finance/CreateInvoice ─────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateInvoiceSelectListsAsync();
            return View(model);
        }

        var result = await _financeService.CreateInvoiceAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Có lỗi khi tạo hóa đơn.");
            await LoadCreateInvoiceSelectListsAsync();
            return View(model);
        }

        TempData["Success"] = $"Đã tạo hóa đơn {result.Data!.InvoiceCode} thành công!";
        return RedirectToAction(nameof(InvoiceDetails), new { id = result.Data.InvoiceId });
    }

    // ─── POST /Finance/GenerateInvoices ───────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInvoices(GenerateInvoicesRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Invoices));
        }

        var result = await _financeService.GenerateInvoicesAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã tạo {result.Data} hóa đơn học phí cho kỳ {model.BillingPeriod}."
                : result.Error;

        return RedirectToAction(nameof(Invoices));
    }

    // ─── POST /Finance/RecordPayment ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RecordPaymentRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu thanh toán không hợp lệ.";
            return RedirectToAction(nameof(InvoiceDetails), new { id = model.InvoiceId });
        }

        var result = await _financeService.RecordPaymentAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(InvoiceDetails), new { id = model.InvoiceId });
        }

        TempData["Success"] = $"Ghi nhận thanh toán {model.AmountPaid:N0}đ thành công.";

        return RedirectToAction(nameof(InvoiceDetails), new { id = model.InvoiceId });
    }

    // ─── GET /Finance/FeeConfigs ──────────────────────────────────────────────

    public async Task<IActionResult> FeeConfigs(int academicYearId)
    {
        var result = await _financeService.GetFeeConfigsAsync(GetCurrentSchoolId(), academicYearId);
        if (!result.IsSuccess)
            return StatusCode(500);

        ViewBag.AcademicYearId = academicYearId;
        return View(result.Data);
    }

    // ─── POST /Finance/CreateFeeConfig ────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeeConfig(CreateFeeConfigRequest model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu cấu hình học phí không hợp lệ.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(FeeConfigs), new { academicYearId = model.AcademicYearId });
        }

        var result = await _financeService.CreateFeeConfigAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã tạo loại phí \"{model.FeeType}\" — {result.Data!.Amount:N0}đ/{model.BillingCycle}."
                : result.Error;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(FeeConfigs), new { academicYearId = model.AcademicYearId });
    }

    // ─── POST /Finance/DeactivateFeeConfig ───────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateFeeConfig(int id, int academicYearId)
    {
        var result = await _financeService.DeactivateFeeConfigAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Đã hủy kích hoạt cấu hình học phí." : result.Error;

        return RedirectToAction(nameof(FeeConfigs), new { academicYearId });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task LoadCreateInvoiceSelectListsAsync()
    {
        var sid = GetCurrentSchoolId();

        // Students
        var students = await _accountService.GetStudentSelectListAsync(sid);
        ViewBag.Students = students.IsSuccess ? students.Data : new List<(int, string, string?)>();

        // Academic years + grade levels
        var years       = await _classService.GetAcademicYearsAsync(sid);
        var gradeLevels = await _classService.GetGradeLevelsAsync(sid);
        ViewBag.AcademicYears = years.IsSuccess ? years.Data
            : new List<LuminaTutors.Application.DTOs.Class.AcademicYearSelectDto>();
        ViewBag.GradeLevels   = gradeLevels.IsSuccess ? gradeLevels.Data
            : new List<LuminaTutors.Application.DTOs.Class.GradeLevelSelectDto>();

        // Fee configs for the active/latest year
        var activeYear = years.Data?.FirstOrDefault(y => y.IsActive) ?? years.Data?.FirstOrDefault();
        if (activeYear != null)
        {
            var configs = await _financeService.GetFeeConfigsAsync(sid, activeYear.AcademicYearId);
            ViewBag.FeeConfigs = configs.IsSuccess ? configs.Data
                : new List<LuminaTutors.Application.DTOs.Finance.TuitionFeeConfigDto>();
        }
        else
        {
            ViewBag.FeeConfigs = new List<LuminaTutors.Application.DTOs.Finance.TuitionFeeConfigDto>();
        }
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
