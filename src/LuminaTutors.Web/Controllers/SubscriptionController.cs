using System.Globalization;
using System.Security.Claims;
using LuminaTutors.Application.DTOs.Subscription;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Web.Services.VnPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Quản lý gói dịch vụ cấp trường: nâng cấp Basic→Premium, mua add-on (Gia Sư AI,
/// Phòng 3D), đăng ký định kỳ tự gia hạn, thanh toán đơn (VNPay hoặc xác nhận thủ công).
/// Hầu hết action chỉ dành cho Admin; riêng <see cref="Locked"/> mở cho mọi người dùng
/// đã đăng nhập (hiển thị khi bị chặn tính năng).
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class SubscriptionController : Controller
{
    private readonly ISubscriptionService _service;
    private readonly IQuotaService        _quota;
    private readonly IConfiguration       _config;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService service, IQuotaService quota, IConfiguration config, ILogger<SubscriptionController> logger)
    {
        _service = service;
        _quota   = quota;   
        _config  = config;
        _logger  = logger;
    }

    private int    SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    private int    UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private bool   VnPayEnabled => _config.GetValue("VnPay:Enabled", true);

    // ── GET /Subscription ─────────────────────────────────────────────────────

    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Index()
    {
        var result = await _service.GetOverviewAsync(SchoolId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new SubscriptionOverviewDto());
        }
        result.Data!.VnPayEnabled = VnPayEnabled;

        var quota = await _quota.GetQuotaStatusAsync(SchoolId);
        ViewBag.Quota = quota.IsSuccess ? quota.Data : null;

        return View(result.Data);
    }

    // ── POST /Subscription/ChangePlan ─────────────────────────────────────────
    // Đăng ký mới hoặc nâng cấp lên gói cao hơn.

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> ChangePlan(ChangePlanRequest request)
    {
        var result = await _service.SubscribeOrUpgradeAsync(SchoolId, UserId, request);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Pay), new { id = result.Data!.OrderId });
    }

    // ── POST /Subscription/BuyAddOn ───────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> BuyAddOn(int addOnId)
    {
        var result = await _service.BuyAddOnAsync(SchoolId, UserId, addOnId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Pay), new { id = result.Data!.OrderId });
    }

    // ── POST /Subscription/BuyQuotaAddOn ──────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> BuyQuotaAddOn(int quotaAddOnId)
    {
        var result = await _service.BuyQuotaAddOnAsync(SchoolId, UserId, quotaAddOnId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Pay), new { id = result.Data!.OrderId });
    }

    // ── POST /Subscription/Renew ──────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Renew()
    {
        var result = await _service.CreateRenewalOrderAsync(SchoolId, UserId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Pay), new { id = result.Data!.OrderId });
    }

    // ── POST /Subscription/SetAutoRenew ───────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> SetAutoRenew(bool enabled)
    {
        var result = await _service.SetAutoRenewAsync(SchoolId, enabled);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? (enabled ? "Đã bật tự động gia hạn (đăng ký định kỳ)." : "Đã tắt tự động gia hạn (mua một lần).")
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subscription/Cancel ─────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Cancel()
    {
        var result = await _service.CancelAsync(SchoolId);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Đã hủy gia hạn. Trường vẫn dùng được đến hết kỳ đã thanh toán."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subscription/RunRenewals — chạy thủ công job đến hạn (cron có thể gọi) ─

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> RunRenewals()
    {
        var count = await _service.ProcessDueRenewalsAsync();
        TempData["Success"] = $"Đã xử lý đăng ký đến hạn. Tạo {count} đơn gia hạn chờ thanh toán.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Subscription/Pay/5 — trang thanh toán cho 1 đơn ──────────────────

    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> Pay(int id)
    {
        var result = await _service.GetOrderAsync(id, SchoolId);
        if (!result.IsSuccess) return NotFound();

        ViewBag.VnPayEnabled = VnPayEnabled;
        return View(result.Data);
    }

    // ── POST /Subscription/ChangeCycle/5 — đổi chu kỳ đơn ngay tại trang thanh toán ─
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> ChangeCycle(int id, SubscriptionCycle cycle)
    {
        var result = await _service.ChangeOrderCycleAsync(id, SchoolId, cycle);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Pay), new { id });
    }

    // ── POST /Subscription/MarkPaid/5 — xác nhận thanh toán thủ công (offline) ─

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var order = await _service.GetOrderAsync(id, SchoolId);
        if (!order.IsSuccess) return NotFound();

        var confirm = await _service.ConfirmOrderPaymentAsync(
            id, order.Data!.Amount, PaymentMethod.BankTransfer,
            $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}", "Xác nhận thủ công bởi quản trị viên");

        TempData[confirm == SubscriptionConfirmResult.Ok || confirm == SubscriptionConfirmResult.AlreadyConfirmed ? "Success" : "Error"] =
            confirm switch
            {
                SubscriptionConfirmResult.Ok               => "Đã xác nhận thanh toán. Gói/tính năng đã được kích hoạt.",
                SubscriptionConfirmResult.AlreadyConfirmed => "Đơn này đã được thanh toán trước đó.",
                SubscriptionConfirmResult.InvalidAmount    => "Số tiền không khớp.",
                SubscriptionConfirmResult.NotFound         => "Không tìm thấy đơn.",
                _                                          => "Có lỗi khi xác nhận thanh toán."
            };
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subscription/BillingEmail — đặt email nhận hóa đơn của trường ───

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> BillingEmail(string? billingEmail)
    {
        var result = await _service.UpdateBillingEmailAsync(SchoolId, billingEmail);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? (string.IsNullOrWhiteSpace(billingEmail)
                ? "Đã xóa email nhận hóa đơn — hóa đơn sẽ gửi về email tài khoản Nhà trường."
                : $"Đã lưu email nhận hóa đơn: {billingEmail.Trim()}")
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subscription/TestEmail — gửi thử để kiểm tra cấu hình SMTP ──────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> TestEmail([FromServices] IBillingEmailService billingMail)
    {
        var result = await billingMail.SendTestEmailAsync(SchoolId);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? $"Đã gửi email thử tới {result.Data}. Kiểm tra hộp thư (kể cả mục Spam)."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Subscription/ResendReceipt/5 — gửi lại hóa đơn qua email ────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> ResendReceipt(int id, [FromServices] IBillingEmailService billingMail)
    {
        // Chặn xem/gửi hóa đơn của trường khác: đơn phải thuộc trường đang đăng nhập.
        var order = await _service.GetOrderAsync(id, SchoolId);
        if (!order.IsSuccess) return NotFound();

        var result = await billingMail.SendSubscriptionReceiptAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? $"Đã gửi hóa đơn {order.Data!.OrderCode} về email nhà trường."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Subscription/PayVnPay/5 — tạo URL VNPay & chuyển hướng ───────────

    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<IActionResult> PayVnPay(int id)
    {
        if (!VnPayEnabled)
        {
            TempData["Error"] = "Thanh toán online (VNPay) chưa được bật.";
            return RedirectToAction(nameof(Pay), new { id });
        }

        var tmnCode    = _config["VnPay:TmnCode"];
        var hashSecret = _config["VnPay:HashSecret"];
        var payUrl     = _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(hashSecret))
        {
            TempData["Error"] = "Cổng thanh toán chưa được cấu hình (TmnCode/HashSecret).";
            return RedirectToAction(nameof(Pay), new { id });
        }

        var order = await _service.GetOrderAsync(id, SchoolId);
        if (!order.IsSuccess) return NotFound();
        if (order.Data!.Status == SubscriptionOrderStatus.Paid.ToString())
        {
            TempData["Success"] = "Đơn này đã được thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        var returnUrl = $"{Request.Scheme}://{Request.Host}/Subscription/VnPayReturn";
        var now    = DateTime.Now;                                  // VNPay dùng giờ Việt Nam
        var txnRef = $"SUB{id}_{now:yyyyMMddHHmmss}";               // prefix SUB để phân biệt với hóa đơn học phí
        var amount = (long)(order.Data.Amount * 100);

        var vnp = new VnPayLibrary();
        vnp.AddRequestData("vnp_Version",    "2.1.0");
        vnp.AddRequestData("vnp_Command",    "pay");
        vnp.AddRequestData("vnp_TmnCode",    tmnCode);
        vnp.AddRequestData("vnp_Amount",     amount.ToString(CultureInfo.InvariantCulture));
        vnp.AddRequestData("vnp_CurrCode",   "VND");
        vnp.AddRequestData("vnp_TxnRef",     txnRef);
        vnp.AddRequestData("vnp_OrderInfo",  $"Thanh toan goi dich vu {order.Data.OrderCode}");
        vnp.AddRequestData("vnp_OrderType",  "other");
        vnp.AddRequestData("vnp_Locale",     "vn");
        vnp.AddRequestData("vnp_ReturnUrl",  returnUrl);
        vnp.AddRequestData("vnp_IpAddr",     HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        vnp.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnp.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        _logger.LogInformation("VNPay create subscription payment cho đơn {Id}, TxnRef={Txn}", id, txnRef);
        return Redirect(vnp.CreateRequestUrl(payUrl, hashSecret));
    }

    // ── GET/POST /Subscription/VnPayIpn — VNPay xác nhận server-to-server ──────
    // Lưu ý: dùng IPN URL riêng cho gói dịch vụ; txnRef có tiền tố "SUB".

    [AllowAnonymous, HttpGet, HttpPost]
    public async Task<IActionResult> VnPayIpn()
    {
        var hashSecret = _config["VnPay:HashSecret"] ?? "";
        var data = Request.Query.Any(q => q.Key.StartsWith("vnp_"))
            ? Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString())
            : (Request.HasFormContentType
                ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString())
                : new Dictionary<string, string>());

        var vnp = new VnPayLibrary();
        foreach (var (k, v) in data)
            if (k.StartsWith("vnp_")) vnp.AddResponseData(k, v);

        if (!vnp.ValidateSignature(data.GetValueOrDefault("vnp_SecureHash"), hashSecret))
            return Json(new { RspCode = "97", Message = "Invalid signature" });

        var orderId = ParseOrderId(vnp.GetResponseData("vnp_TxnRef"));
        if (orderId is null)
            return Json(new { RspCode = "01", Message = "Order not found" });

        var amount   = long.TryParse(vnp.GetResponseData("vnp_Amount"), out var a) ? a / 100m : -1m;
        var respCode = vnp.GetResponseData("vnp_ResponseCode");
        var tranStat = vnp.GetResponseData("vnp_TransactionStatus");

        if (respCode != "00" || tranStat != "00")
            return Json(new { RspCode = "00", Message = "Confirm Success" }); // ACK, không kích hoạt

        var result = await _service.ConfirmOrderPaymentAsync(
            orderId.Value, amount, PaymentMethod.VnPay,
            vnp.GetResponseData("vnp_TransactionNo"), Request.QueryString.Value);

        var (rsp, msg) = result switch
        {
            SubscriptionConfirmResult.Ok               => ("00", "Confirm Success"),
            SubscriptionConfirmResult.AlreadyConfirmed => ("02", "Order already confirmed"),
            SubscriptionConfirmResult.NotFound         => ("01", "Order not found"),
            SubscriptionConfirmResult.InvalidAmount    => ("04", "Invalid amount"),
            _                                          => ("99", "Unknown error"),
        };
        return Json(new { RspCode = rsp, Message = msg });
    }

    // ── GET /Subscription/VnPayReturn — người dùng quay về từ VNPay ────────────

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> VnPayReturn()
    {
        var hashSecret = _config["VnPay:HashSecret"] ?? "";
        var vnp = new VnPayLibrary();
        foreach (var (k, v) in Request.Query)
            if (k.StartsWith("vnp_")) vnp.AddResponseData(k, v.ToString());

        if (!vnp.ValidateSignature(Request.Query["vnp_SecureHash"].ToString(), hashSecret))
        {
            TempData["Error"] = "Chữ ký không hợp lệ — không xác thực được giao dịch.";
            return RedirectToAction(nameof(Index));
        }

        var ok = vnp.GetResponseData("vnp_ResponseCode") == "00"
              && vnp.GetResponseData("vnp_TransactionStatus") == "00";
        if (!ok)
        {
            TempData["Error"] = "Thanh toán không thành công hoặc đã bị hủy.";
            return RedirectToAction(nameof(Index));
        }

        // Kích hoạt ngay khi người dùng quay về, không chờ IPN (quan trọng khi chạy
        // localhost / chưa khai báo IPN URL — VNPay không gọi server-to-server được).
        // ConfirmOrderPaymentAsync idempotent: nếu IPN đã xác nhận trước đó → AlreadyConfirmed (no-op).
        var orderId = ParseOrderId(vnp.GetResponseData("vnp_TxnRef"));
        var amount  = long.TryParse(vnp.GetResponseData("vnp_Amount"), out var a) ? a / 100m : -1m;

        if (orderId is null)
        {
            TempData["Error"] = "Thanh toán thành công nhưng không xác định được đơn để kích hoạt. Vui lòng liên hệ hỗ trợ.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _service.ConfirmOrderPaymentAsync(
            orderId.Value, amount, PaymentMethod.VnPay,
            vnp.GetResponseData("vnp_TransactionNo"), Request.QueryString.Value);

        TempData[result is SubscriptionConfirmResult.Ok or SubscriptionConfirmResult.AlreadyConfirmed ? "Success" : "Error"] =
            result switch
            {
                SubscriptionConfirmResult.Ok               => "Thanh toán thành công. Gói/tính năng đã được kích hoạt.",
                SubscriptionConfirmResult.AlreadyConfirmed => "Thanh toán thành công. Gói/tính năng đã được kích hoạt.",
                SubscriptionConfirmResult.InvalidAmount    => "Thanh toán thành công nhưng số tiền không khớp. Vui lòng liên hệ hỗ trợ.",
                SubscriptionConfirmResult.NotFound         => "Không tìm thấy đơn tương ứng để kích hoạt.",
                _                                          => "Thanh toán thành công nhưng kích hoạt gói gặp lỗi. Vui lòng thử lại hoặc liên hệ hỗ trợ."
            };
        return RedirectToAction(nameof(Index));
    }

    // ═══ E-Selling admin (SYSADMIN) ═══════════════════════════════════════════

    // ── GET /Subscription/Catalog — quản lý gói & add-on ──────────────────────
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Catalog()
    {
        var result = await _service.GetCatalogAsync();
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new CatalogDto());
        }
        return View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> SavePlan(PlanEditRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu gói không hợp lệ.";
            return RedirectToAction(nameof(Catalog));
        }
        var result = await _service.SavePlanAsync(request);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã lưu gói dịch vụ." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> TogglePlan(int id)
    {
        var result = await _service.TogglePlanActiveAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã cập nhật trạng thái gói." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> SaveAddOn(AddOnEditRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu add-on không hợp lệ.";
            return RedirectToAction(nameof(Catalog));
        }
        var result = await _service.SaveAddOnAsync(request);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã lưu add-on." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> ToggleAddOn(int id)
    {
        var result = await _service.ToggleAddOnActiveAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã cập nhật trạng thái add-on." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> SaveQuotaAddOn(QuotaAddOnEditRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu gói mở rộng không hợp lệ.";
            return RedirectToAction(nameof(Catalog));
        }
        var result = await _service.SaveQuotaAddOnAsync(request);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã lưu gói mở rộng quota." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> ToggleQuotaAddOn(int id)
    {
        var result = await _service.ToggleQuotaAddOnActiveAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Đã cập nhật trạng thái gói mở rộng." : result.Error;
        return RedirectToAction(nameof(Catalog));
    }

    // ── GET /Subscription/Subscriptions — doanh thu + đăng ký của tất cả trường ─
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Subscriptions()
    {
        var subs    = await _service.GetAllSubscriptionsAsync();
        var revenue = await _service.GetRevenueReportAsync();
        var catalog = await _service.GetCatalogAsync();
        var paid    = await _service.GetPaidOrdersAsync();
        ViewBag.OwnSchoolId = SchoolId;            // để view đánh số ID hiển thị (trường hệ thống = 0)
        return View(new AllSubscriptionsVm
        {
            Revenue = revenue.IsSuccess ? revenue.Data! : new RevenueReportDto(),
            Rows    = subs.IsSuccess ? subs.Data!.ToList() : new List<SchoolSubscriptionRowDto>(),
            Plans   = catalog.IsSuccess
                ? catalog.Data!.Plans.Where(p => p.IsActive).Select(p => new PlanOptionDto(p.PlanId, p.Name)).ToList()
                : new List<PlanOptionDto>(),
            PaidOrderList = paid.IsSuccess ? paid.Data!.ToList() : new List<PaidOrderDto>()
        });
    }

    // ── POST /Subscription/AdminChangePlan — SYSADMIN điều chỉnh gói cho 1 trường ─
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> AdminChangePlan(int schoolId, int planId, SubscriptionCycle cycle, bool autoRenew)
    {
        var r = await _service.AdminChangePlanAsync(schoolId, planId, cycle, autoRenew, UserId);
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess
            ? "Đã điều chỉnh gói dịch vụ cho trường."
            : r.Error;
        return RedirectToAction(nameof(Subscriptions));
    }

    // ── POST /Subscription/OnboardSchool — tạo trường + tài khoản Nhà trường ──
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> OnboardSchool(OnboardSchoolRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu chưa hợp lệ. Kiểm tra tên trường, email và mật khẩu (≥ 6 ký tự).";
            return RedirectToAction(nameof(Subscriptions));
        }
        var result = await _service.OnboardSchoolAsync(request);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? $"Đã tạo trường \"{request.SchoolName}\" và tài khoản Nhà trường ({request.AdminEmail})."
            : result.Error;
        return RedirectToAction(nameof(Subscriptions));
    }

    // ── GET /Subscription/Schools — quản lý tài khoản các trường (CRUD) ───────
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> Schools()
    {
        var result = await _service.GetSchoolsAsync();
        ViewBag.OwnSchoolId = SchoolId;
        return View(result.IsSuccess ? result.Data : new List<SchoolAccountDto>());
    }

    // ── GET /Subscription/SchoolDetail/5 — chi tiết 1 trường (thành viên + xếp loại) ─
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> SchoolDetail(int id)
    {
        var result = await _service.GetSchoolDetailAsync(id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Schools));
        }
        return View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> UpdateSchool(UpdateSchoolRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu chưa hợp lệ. Kiểm tra tên trường và email.";
            return RedirectToAction(nameof(Schools));
        }
        var r = await _service.UpdateSchoolAsync(request);
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Đã cập nhật thông tin trường." : r.Error;
        return RedirectToAction(nameof(Schools));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> ToggleSchoolActive(int id)
    {
        var r = await _service.ToggleSchoolActiveAsync(id, SchoolId);
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Đã đổi trạng thái trường." : r.Error;
        return RedirectToAction(nameof(Schools));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> ResetSchoolPassword(int id, string? newPassword)
    {
        var r = await _service.ResetSchoolAdminPasswordAsync(id, newPassword ?? "");
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Đã đặt lại mật khẩu Nhà trường." : r.Error;
        return RedirectToAction(nameof(Schools));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> DeleteSchool(int id)
    {
        var r = await _service.DeleteSchoolAsync(id, SchoolId);
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Đã xóa trường." : r.Error;
        return RedirectToAction(nameof(Schools));
    }

    // ── GET /Subscription/Locked?feature=AiTutor — trang chặn tính năng ───────

    [Authorize(Policy = "AnyAuthenticated")]
    public IActionResult Locked(string? feature)
    {
        ViewBag.Feature = feature switch
        {
            nameof(PremiumFeature.AiTutor)    => "Gia Sư AI",
            nameof(PremiumFeature.VirtualLab) => "Phòng học 3D",
            _                                 => "tính năng cao cấp"
        };
        ViewBag.IsAdmin = User.IsInRole("ADMIN");
        return View();
    }

    // ── GET /Subscription/Inactive — trường chưa có gói đang hoạt động ─────────
    // Đích chuyển hướng của RequireActiveSubscriptionFilter. Mở cho mọi vai trò để
    // giáo viên/học sinh cũng thấy thông báo; admin có nút đăng ký gói.

    [Authorize(Policy = "AnyAuthenticated")]
    public IActionResult Inactive()
    {
        ViewBag.IsAdmin = User.IsInRole("ADMIN");
        return View();
    }

    private static int? ParseOrderId(string? txnRef)
    {
        if (string.IsNullOrWhiteSpace(txnRef)) return null;
        var head = txnRef.Split('_')[0];
        if (head.StartsWith("SUB", StringComparison.OrdinalIgnoreCase))
            head = head[3..];
        return int.TryParse(head, out var id) ? id : null;
    }
}
