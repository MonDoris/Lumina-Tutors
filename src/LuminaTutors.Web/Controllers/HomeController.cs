using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Public landing page — no authentication required.
/// </summary>
public sealed class HomeController : Controller
{
    [HttpGet("/")]
    [HttpGet("/home")]
    public IActionResult Index()
    {
        // Redirect already-authenticated users to dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }
}
