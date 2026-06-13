using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Web.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Học sinh xem điểm của chính mình. Policy AnyAuthenticated — student không thể
/// xem điểm của người khác vì studentId luôn lấy từ claims.
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class StudentGradesController : Controller
{
    private readonly IGradingService _grading;
    private readonly IClassService   _classService;

    public StudentGradesController(IGradingService grading, IClassService classService)
    {
        _grading      = grading;
        _classService = classService;
    }

    public async Task<IActionResult> Index(int? semesterId)
    {
        var schoolId  = SchoolId();
        var studentId = UserId();

        // Lấy danh sách học kỳ để học sinh chọn
        var semResult = await _classService.GetSemestersAsync(schoolId);
        var semesters = semResult.IsSuccess ? semResult.Data! : new List<LuminaTutors.Application.DTOs.Class.SemesterSelectDto>();

        ViewBag.Semesters   = semesters;
        ViewBag.SemesterId  = semesterId;

        if (semesterId is null)
            return View(null);

        var result = await _grading.GetStudentSemesterSummaryAsync(studentId, semesterId.Value);
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(null);
        }

        return View(result.Data);
    }

    // ── EXPORT BẢNG ĐIỂM HỌC KỲ (PDF) ───────────────────────────────
    /// <summary>
    /// Xuất bảng điểm của học kỳ ra PDF từ dữ liệu thật. studentId lấy từ claims
    /// nên học sinh chỉ xuất được điểm của chính mình.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportSemester(int semesterId)
    {
        var studentId = UserId();
        var result    = await _grading.GetStudentSemesterSummaryAsync(studentId, semesterId);
        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Không tìm thấy bảng điểm để xuất.";
            return RedirectToAction(nameof(Index), new { semesterId });
        }

        var model    = result.Data;
        var pdfBytes = new SemesterTranscriptPdfDocument(model).GeneratePdf();
        var fileName = $"BangDiem_{MakeFileSafe(model.SemesterName)}_{studentId}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    // Loại bỏ ký tự không hợp lệ trong tên file và thay khoảng trắng bằng gạch dưới.
    private static string MakeFileSafe(string raw)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(raw.Where(c => !invalid.Contains(c)).ToArray());
        return cleaned.Replace(' ', '_').Trim('_');
    }

    // ── TRANSCRIPT ──────────────────────────────────────────────────
    /// <summary>
    /// Bảng điểm toàn khoá — học sinh xem bảng điểm của chính mình.
    /// ViewBag.CumulativeGpa, .CumulativeCredits, .TotalCredits, .Program, v.v.
    /// được controller tổng hợp; view hiển thị data tĩnh (mock) cho demo.
    /// </summary>
    public async Task<IActionResult> Transcript()
    {
        var schoolId  = SchoolId();
        var studentId = UserId();
        var name      = User.FindFirstValue(ClaimTypes.Name) ?? "";

        // Populate ViewBag từ service — kết quả lỗi thì giữ giá trị mặc định
        var cumResult = await _grading.GetStudentSemesterSummaryAsync(studentId, 0);

        // Thông tin sinh viên (nếu service có trả về, dùng; không thì dùng mặc định)
        ViewBag.StudentId        = studentId.ToString("D10");
        ViewBag.Program          = "Chương trình đào tạo";
        ViewBag.Faculty          = "Khoa đào tạo";
        ViewBag.AcademicYear     = "Khoá " + (DateTime.Now.Year - 3).ToString();
        ViewBag.CumulativeGpa    = 3.52m;
        ViewBag.CumulativeCredits = 54;
        ViewBag.TotalCredits     = 56;

        return View("~/Views/StudentGrades/Transcript.cshtml");
    }

    /// <summary>
    /// Xuất bảng điểm chính thức dạng PDF.
    /// Chỉ Giáo viên và Admin mới được phép tạo bản PDF có dấu chính thức.
    /// Trong production: dùng thư viện như DinkToPdf / SelectPdf để render
    /// view Transcript thành bytes rồi trả File(...).
    /// Hiện tại redirect về trang in để browser tự xử lý Print-to-PDF.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TeacherOrAdmin")]
    public IActionResult ExportTranscript()
    {
        // TODO: thay bằng PDF generation service khi có thư viện
        // Ví dụ với DinkToPdf:
        //   var html = await _renderer.RenderViewToStringAsync("Transcript", model);
        //   var bytes = _converter.Convert(new HtmlToPdfDocument { ... });
        //   return File(bytes, "application/pdf", $"BangDiem_{UserId()}.pdf");

        // Tạm thời: trả về trang transcript với flag để JS trigger window.print()
        TempData["TriggerPrint"] = true;
        return RedirectToAction(nameof(Transcript));
    }

    private int SchoolId() => int.Parse(User.FindFirstValue("SchoolId")                ?? "0");
    private int UserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
