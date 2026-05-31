using System.Security.Claims;
using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

[Authorize(Policy = "FinanceAccess")]
public sealed class FinanceController : Controller
{
    private readonly IFinanceService _financeService;
    private readonly ILogger<FinanceController> _logger;

    public FinanceController(IFinanceService financeService, ILogger<FinanceController> logger)
    {
        _financeService = financeService;
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
    public async Task<IActionResult> CreateFeeConfig(CreateFeeConfigRequest model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu cấu hình học phí không hợp lệ.";
            return RedirectToAction(nameof(FeeConfigs), new { academicYearId = model.AcademicYearId });
        }

        var result = await _financeService.CreateFeeConfigAsync(
            GetCurrentSchoolId(), GetCurrentUserId(), model);

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? $"Đã tạo cấu hình học phí {result.Data!.Amount:N0}đ/kỳ cho khối {model.GradeLevelId}."
                : result.Error;

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

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private int GetCurrentSchoolId() =>
        int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
