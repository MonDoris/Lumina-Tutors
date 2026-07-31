using System.Globalization;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Web.Services.VnPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Thanh toán học phí online qua VNPay.
/// • GET  /Payment/Create/{id} : tạo URL VNPay cho hóa đơn rồi redirect (cần đăng nhập).
/// • GET  /Payment/Ipn         : VNPay gọi server-to-server → xác nhận & cập nhật hóa đơn (ẩn danh).
/// • GET  /Payment/Return      : VNPay redirect người dùng về → trang kết quả (ẩn danh).
/// </summary>
public sealed class PaymentController : Controller
{
    private readonly IFinanceService _finance;
    private readonly IConfiguration  _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IFinanceService finance, IConfiguration config, ILogger<PaymentController> logger)
    {
        _finance = finance;
        _config  = config;
        _logger  = logger;
    }

    // ── GET /Payment/Create/5 — tạo URL & chuyển sang VNPay ───────────────────
    [Authorize(Policy = "AnyAuthenticated")]
    [HttpGet]
    public async Task<IActionResult> Create(int id)
    {
        if (!_config.GetValue("VnPay:Enabled", false))
            return ResultPage(false, "Thanh toán online chưa được bật. Vui lòng liên hệ kế toán.");

        var tmnCode    = _config["VnPay:TmnCode"];
        var hashSecret = _config["VnPay:HashSecret"];
        var payUrl     = _config["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(hashSecret))
            return ResultPage(false, "Cổng thanh toán chưa được cấu hình (TmnCode/HashSecret).");

        var inv = await _finance.GetInvoiceAsync(id);
        if (!inv.IsSuccess || inv.Data is null)
            return ResultPage(false, "Không tìm thấy hóa đơn.");
        if (inv.Data.Status == InvoiceStatus.Paid.ToString())
            return ResultPage(true, $"Hóa đơn {inv.Data.InvoiceCode} đã được thanh toán.");

        var returnUrl = _config["VnPay:ReturnUrl"];
        if (string.IsNullOrWhiteSpace(returnUrl))
            returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/Return";

        var now    = DateTime.Now;                       // VNPay dùng giờ Việt Nam (GMT+7)
        var txnRef = $"{id}_{now:yyyyMMddHHmmss}";        // duy nhất trong ngày + map ngược về hóa đơn
        var amount = (long)(inv.Data.FinalAmount * 100);

        var vnp = new VnPayLibrary();
        vnp.AddRequestData("vnp_Version",   "2.1.0");
        vnp.AddRequestData("vnp_Command",   "pay");
        vnp.AddRequestData("vnp_TmnCode",   tmnCode);
        vnp.AddRequestData("vnp_Amount",    amount.ToString(CultureInfo.InvariantCulture));
        vnp.AddRequestData("vnp_CurrCode",  "VND");
        vnp.AddRequestData("vnp_TxnRef",    txnRef);
        vnp.AddRequestData("vnp_OrderInfo", $"Thanh toan hoc phi {inv.Data.InvoiceCode}");
        vnp.AddRequestData("vnp_OrderType", "billpayment");
        vnp.AddRequestData("vnp_Locale",    "vn");
        vnp.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnp.AddRequestData("vnp_IpAddr",    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        vnp.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnp.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        var url = vnp.CreateRequestUrl(payUrl, hashSecret);
        _logger.LogInformation("VNPay create payment cho hóa đơn {Id}, TxnRef={Txn}", id, txnRef);
        return Redirect(url);
    }

    // ── GET/POST /Payment/Ipn — VNPay xác nhận server-to-server ───────────────
    [AllowAnonymous]
    [HttpGet, HttpPost]
    public async Task<IActionResult> Ipn()
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

        var inputHash = data.GetValueOrDefault("vnp_SecureHash");
        if (!vnp.ValidateSignature(inputHash, hashSecret))
            return Json(new { RspCode = "97", Message = "Invalid signature" });

        var txnRef = vnp.GetResponseData("vnp_TxnRef") ?? "";
        if (!int.TryParse(txnRef.Split('_')[0], out var invoiceId))
            return Json(new { RspCode = "01", Message = "Order not found" });

        var amount = long.TryParse(vnp.GetResponseData("vnp_Amount"), out var a) ? a / 100m : -1m;
        var respCode  = vnp.GetResponseData("vnp_ResponseCode");
        var tranStat  = vnp.GetResponseData("vnp_TransactionStatus");

        // Giao dịch thất bại/hủy: vẫn ACK để VNPay ngừng retry, không cập nhật hóa đơn.
        if (respCode != "00" || tranStat != "00")
        {
            _logger.LogInformation("VNPay IPN giao dịch không thành công Invoice={Id} Code={Code}", invoiceId, respCode);
            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        var result = await _finance.ConfirmVnPayPaymentAsync(
            invoiceId, amount, vnp.GetResponseData("vnp_TransactionNo"), Request.QueryString.Value);

        var (rsp, msg) = result switch
        {
            VnPayConfirmResult.Ok               => ("00", "Confirm Success"),
            VnPayConfirmResult.AlreadyConfirmed => ("02", "Order already confirmed"),
            VnPayConfirmResult.NotFound         => ("01", "Order not found"),
            VnPayConfirmResult.InvalidAmount    => ("04", "Invalid amount"),
            _                                   => ("99", "Unknown error"),
        };
        return Json(new { RspCode = rsp, Message = msg });
    }

    // ── GET /Payment/Return — người dùng quay về từ VNPay ─────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Return()
    {
        var hashSecret = _config["VnPay:HashSecret"] ?? "";
        var vnp = new VnPayLibrary();
        foreach (var (k, v) in Request.Query)
            if (k.StartsWith("vnp_")) vnp.AddResponseData(k, v.ToString());

        var inputHash = Request.Query["vnp_SecureHash"].ToString();
        if (!vnp.ValidateSignature(inputHash, hashSecret))
            return ResultPage(false, "Chữ ký không hợp lệ — không xác thực được giao dịch.");

        var ok = vnp.GetResponseData("vnp_ResponseCode") == "00"
              && vnp.GetResponseData("vnp_TransactionStatus") == "00";
        var amount = long.TryParse(vnp.GetResponseData("vnp_Amount"), out var a) ? (a / 100m) : 0m;

        if (!ok)
            return ResultPage(false, "Thanh toán không thành công hoặc đã bị hủy.");

        // Xác nhận & cập nhật hóa đơn NGAY khi người dùng quay về, không chờ IPN
        // (quan trọng khi chạy localhost/chưa khai báo IPN URL công khai).
        // ConfirmVnPayPaymentAsync idempotent: nếu IPN đã xác nhận trước đó → AlreadyConfirmed.
        var txnRef = vnp.GetResponseData("vnp_TxnRef") ?? "";
        if (int.TryParse(txnRef.Split('_')[0], out var invoiceId))
        {
            var confirm = await _finance.ConfirmVnPayPaymentAsync(
                invoiceId, amount, vnp.GetResponseData("vnp_TransactionNo"), Request.QueryString.Value);

            if (confirm is VnPayConfirmResult.Ok or VnPayConfirmResult.AlreadyConfirmed)
                return ResultPage(true, $"Thanh toán thành công {amount:N0}đ. Hóa đơn đã được cập nhật. Cảm ơn bạn!");
            if (confirm == VnPayConfirmResult.InvalidAmount)
                return ResultPage(false, "Thanh toán thành công nhưng số tiền không khớp hóa đơn. Vui lòng liên hệ nhà trường.");
        }

        return ResultPage(true, $"Thanh toán thành công {amount:N0}đ. Cảm ơn bạn! Hệ thống sẽ cập nhật hóa đơn trong giây lát.");
    }

    // ── Trang kết quả đơn giản (đứng riêng, không cần layout/đăng nhập) ───────
    private ContentResult ResultPage(bool ok, string message)
    {
        // Phụ huynh không có quyền vào /Finance — cho họ quay về trang học phí của mình.
        var (backUrl, backLabel) = User.IsInRole("PARENT")
            ? ("/Parent/Tuition", "Về học phí của tôi")
            : ("/Finance/Invoices", "Về danh sách hóa đơn");

        var color = ok ? "#34a853" : "#ea4335";
        var icon  = ok ? "✅" : "⚠️";
        var html =
            $"<!DOCTYPE html><html lang='vi'><head><meta charset='utf-8'>" +
            "<meta name='viewport' content='width=device-width,initial-scale=1'>" +
            "<title>Kết quả thanh toán — Lumina Tutors</title></head>" +
            "<body style='font-family:system-ui,sans-serif;background:#f1f5f9;margin:0;display:flex;" +
            "min-height:100vh;align-items:center;justify-content:center;padding:20px'>" +
            "<div style='background:#fff;border-radius:16px;box-shadow:0 10px 40px rgba(0,0,0,.12);" +
            "max-width:420px;width:100%;padding:32px;text-align:center'>" +
            $"<div style='font-size:3rem'>{icon}</div>" +
            $"<h2 style='color:{color};margin:12px 0 8px;font-size:1.2rem'>{(ok ? "Thành công" : "Thông báo")}</h2>" +
            $"<p style='color:#475569;line-height:1.6;font-size:.95rem'>{message}</p>" +
            $"<a href='{backUrl}' style='display:inline-block;margin-top:18px;background:#1a73e8;" +
            $"color:#fff;text-decoration:none;padding:11px 22px;border-radius:10px;font-weight:600'>{backLabel}</a>" +
            "</div></body></html>";
        return Content(html, "text/html");
    }
}
