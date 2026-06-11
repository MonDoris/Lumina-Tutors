using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// "Lumina Holographic Nexus" — phòng học 3D real-time + streaming.
/// View là một trang toàn màn hình (Layout = null) host &lt;canvas&gt; Three.js,
/// render hình chiếu hologram, overlay Glassmorphism tối, và kết nối SignalR
/// tới /hubs/lumina-rtc (SFU thuần C#).
///
/// Giáo viên chọn MÔN HỌC + THÍ NGHIỆM ngay trong phòng; mặc định mở đúng môn
/// giáo viên đang dạy (tự dò qua hồ sơ tài khoản).
/// </summary>
[Authorize(Policy = "LabAccess")]
public sealed class LuminaNexusController : Controller
{
    private readonly IAccountService _accountService;

    public LuminaNexusController(IAccountService accountService)
        => _accountService = accountService;

    // Bộ thẻ môn học dùng trong phòng Lab 3D (đồng bộ với VirtualLab)
    private static readonly string[] ValidTags = ["chemistry", "physics", "biology", "math"];

    private static readonly (string[] Keywords, string Tag)[] SubjectTagMap =
    [
        (["hóa", "hoá", "chemistry"], "chemistry"),
        (["vật lý", "vat ly", "physics"], "physics"),
        (["sinh", "biology"], "biology"),
        (["toán", "toan", "math"], "math"),
    ];

    private static string? GuessSubjectTag(string? subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName)) return null;
        var lower = subjectName.ToLowerInvariant();
        foreach (var (keywords, tag) in SubjectTagMap)
            if (keywords.Any(k => lower.Contains(k)))
                return tag;
        return null;
    }

    // ─── GET /LuminaNexus?room=demo&subject=biology&scene=cell ────────────────
    public async Task<IActionResult> Index(string room = "nexus-demo", string? subject = null, string? scene = null)
    {
        var isTeacher = User.IsInRole("TEACHER") || User.IsInRole("ADMIN");

        ViewBag.RoomId      = string.IsNullOrWhiteSpace(room) ? "nexus-demo" : room.Trim();
        ViewBag.IsTeacher   = isTeacher;
        ViewBag.DisplayName = User.FindFirstValue(ClaimTypes.Name) ?? "Học viên";

        // Môn học mặc định: ưu tiên tham số → môn giáo viên đang dạy → null (UI fallback)
        var subjectTag = ValidTags.Contains(subject) ? subject : null;
        if (subjectTag is null && isTeacher)
        {
            var schoolId = int.Parse(User.FindFirstValue("SchoolId") ?? "0");
            var userId   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var profile  = await _accountService.GetAccountByIdAsync(schoolId, userId);
            if (profile.IsSuccess)
                subjectTag = GuessSubjectTag(profile.Data!.PrimarySubjectName ?? profile.Data.SpecializationSubject);
        }

        ViewBag.DefaultSubjectTag = subjectTag;
        ViewBag.InitialScene      = string.IsNullOrWhiteSpace(scene) ? null : scene.Trim();

        return View();
    }
}
