using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LuminaTutors.Web.Filters;

/// <summary>
/// Chặn truy cập một action/controller nếu trường (tenant) chưa mở khóa tính năng
/// cao cấp tương ứng qua gói dịch vụ hoặc add-on.
/// • Yêu cầu điều hướng (GET) → redirect tới trang nâng cấp /Subscription/Locked.
/// • Yêu cầu AJAX / không phải GET → trả 403 JSON { locked = true }.
/// Đặt SAU [Authorize] — nếu chưa đăng nhập sẽ để pipeline auth xử lý trước.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireFeatureAttribute : TypeFilterAttribute
{
    public RequireFeatureAttribute(PremiumFeature feature) : base(typeof(RequireFeatureFilter))
    {
        Arguments = new object[] { feature };
    }
}

internal sealed class RequireFeatureFilter : IAsyncAuthorizationFilter
{
    private readonly PremiumFeature       _feature;
    private readonly ISubscriptionService _subscriptions;

    public RequireFeatureFilter(PremiumFeature feature, ISubscriptionService subscriptions)
    {
        _feature       = feature;
        _subscriptions = subscriptions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
            return; // chưa đăng nhập → để [Authorize] / cookie middleware xử lý

        if (!int.TryParse(user.FindFirstValue("SchoolId"), out var schoolId) || schoolId == 0)
            return;

        if (await _subscriptions.HasFeatureAsync(schoolId, _feature))
            return; // đã mở khóa → cho qua

        var req = context.HttpContext.Request;
        var wantsJson =
            string.Equals(req.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
            req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
            !HttpMethods.IsGet(req.Method);

        if (wantsJson)
        {
            context.Result = new JsonResult(new
            {
                error   = "Tính năng này chưa được mở khóa trong gói dịch vụ của trường.",
                locked  = true,
                feature = _feature.ToString()
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
        else
        {
            context.Result = new RedirectToActionResult(
                "Locked", "Subscription", new { feature = _feature.ToString() });
        }
    }
}
