using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LuminaTutors.Web.Filters;

/// <summary>
/// Cổng toàn cục (multi-tenant): chặn MỌI chức năng lõi nếu trường (tenant) chưa có
/// gói dịch vụ đang hoạt động — kể cả gói Cơ Bản. Áp cho mọi vai trò trong trường.
///
/// Luôn chừa các controller thiết yếu để admin còn vào đăng ký/thanh toán (tránh tự
/// khóa chính mình), và bỏ qua hoàn toàn SYSADMIN (bên bán, không thuộc trường nào)
/// cùng các API mobile (xác thực JWT, gating riêng).
///
/// • GET             → chuyển hướng tới /Subscription/Inactive.
/// • AJAX / non-GET  → 403 JSON { subscriptionRequired = true }.
/// </summary>
internal sealed class RequireActiveSubscriptionFilter : IAsyncAuthorizationFilter
{
    private readonly ISubscriptionService _subscriptions;

    // Controller truy cập được kể cả khi chưa có gói (đăng nhập, mua gói, tài khoản, trang chủ,
    // callback thanh toán). Khớp theo tên controller, không phân biệt hoa thường.
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Auth", "Account", "Subscription", "Home", "Payment"
    };

    public RequireActiveSubscriptionFilter(ISubscriptionService subscriptions)
        => _subscriptions = subscriptions;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
            return; // chưa đăng nhập → để pipeline auth/cookie xử lý trước

        if (user.IsInRole("SYSADMIN"))
            return; // bên bán, không bị ràng buộc gói

        if (context.ActionDescriptor is not ControllerActionDescriptor cad)
            return;

        // Bỏ qua controller thiết yếu và toàn bộ API mobile
        if (Allowed.Contains(cad.ControllerName) ||
            (cad.ControllerTypeInfo.Namespace?.Contains("Controllers.Api", StringComparison.Ordinal) ?? false))
            return;

        if (!int.TryParse(user.FindFirstValue("SchoolId"), out var schoolId) || schoolId == 0)
            return; // không xác định được trường → không khóa

        if (await _subscriptions.HasActiveSubscriptionAsync(schoolId))
            return; // có gói đang hoạt động → cho qua

        var req = context.HttpContext.Request;
        var wantsJson =
            string.Equals(req.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
            req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
            !HttpMethods.IsGet(req.Method);

        if (wantsJson)
        {
            context.Result = new JsonResult(new
            {
                error = "Trường chưa đăng ký gói dịch vụ. Vui lòng liên hệ Nhà trường để kích hoạt.",
                subscriptionRequired = true
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
        else
        {
            context.Result = new RedirectToActionResult("Inactive", "Subscription", null);
        }
    }
}
