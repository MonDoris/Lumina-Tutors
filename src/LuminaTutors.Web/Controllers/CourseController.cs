using System.Security.Claims;
using LuminaTutors.Application.DTOs.Course;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// E-Learning — khóa học.
/// • GIÁO VIÊN: soạn nội dung (Detail là course builder — module/lesson/gán lớp
///   chạy qua endpoint JSON: fetch + RequestVerificationToken header).
/// • NHÀ TRƯỜNG (ADMIN): chỉ GIÁM SÁT — Manage (theo giáo viên) → ClassProgress
///   (từng lớp: bài tập &amp; % hoàn thành). Mọi endpoint ghi đều gắn policy
///   "CourseAuthoring" (chỉ TEACHER) nên nhà trường không tạo/sửa được khóa.
/// </summary>
[Authorize(Policy = "TeacherOrAdmin")]
public sealed class CourseController : Controller
{
    private readonly ICourseService _svc;
    private readonly IClassService  _classSvc;
    private readonly ILogger<CourseController> _logger;

    public CourseController(ICourseService svc, IClassService classSvc, ILogger<CourseController> logger)
    {
        _svc      = svc;
        _classSvc = classSvc;
        _logger   = logger;
    }

    private int  UserId    => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int  SchoolId  => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
    /// <summary>Chỉ giáo viên mới được soạn nội dung; các vai trò khác chỉ giám sát.</summary>
    private bool IsAuthor  => User.IsInRole("TEACHER");

    private IActionResult JsonResult<T>(LuminaTutors.Domain.Common.Result<T> result) =>
        result.IsSuccess ? Json(new { ok = true, data = result.Data })
                         : Json(new { ok = false, error = result.Error });

    private IActionResult JsonResult(LuminaTutors.Domain.Common.Result result) =>
        result.IsSuccess ? Json(new { ok = true })
                         : Json(new { ok = false, error = result.Error });

    // ══ Pages ═════════════════════════════════════════════════════════════════

    // GET /Course — danh sách khóa học của trường (giáo viên soạn nội dung)
    public async Task<IActionResult> Index()
    {
        if (!IsAuthor) return RedirectToAction(nameof(Manage));

        var result = await _svc.GetCoursesAsync(SchoolId);
        ViewBag.Courses = result.IsSuccess ? result.Data : new List<CourseListItemDto>();
        return View();
    }

    // GET /Course/Manage — nhà trường: khóa học theo giáo viên → lớp
    public async Task<IActionResult> Manage()
    {
        var result = await _svc.GetTeacherCourseOverviewAsync(SchoolId);
        ViewBag.Groups = result.IsSuccess ? result.Data : new List<TeacherCourseGroupDto>();
        return View();
    }

    // GET /Course/ClassProgress?courseId=5&classId=3 — bài tập & % của 1 lớp
    public async Task<IActionResult> ClassProgress(int courseId, int classId)
    {
        var result = await _svc.GetClassCourseProgressAsync(SchoolId, courseId, classId);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Manage)); }

        ViewBag.Progress = result.Data;
        return View();
    }

    // GET /Course/Detail/5 — course builder (chỉ giáo viên)
    [Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await _svc.GetCourseDetailAsync(SchoolId, id);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }

        var classes  = await _svc.GetAssignableClassesAsync(SchoolId);
        var quizzes  = await _svc.GetLinkableQuizExamsAsync(SchoolId);
        var assigns  = await _svc.GetClassAssignmentsAsync(SchoolId, id);
        var subjects = await _classSvc.GetSubjectsAsync(SchoolId);
        var grades   = await _classSvc.GetGradeLevelsAsync(SchoolId);
        var stats    = await _svc.GetCourseStatsAsync(SchoolId, id);

        ViewBag.Stats       = stats.IsSuccess ? stats.Data : null;
        ViewBag.Course      = result.Data;
        ViewBag.Classes     = classes.IsSuccess ? classes.Data : new List<ClassOptionDto>();
        ViewBag.Quizzes     = quizzes.IsSuccess ? quizzes.Data : new List<QuizExamOptionDto>();
        ViewBag.Assignments = assigns.IsSuccess ? assigns.Data : new List<ClassAssignmentDto>();
        ViewBag.Subjects    = subjects.IsSuccess ? subjects.Data : null;
        ViewBag.GradeLevels = grades.IsSuccess ? grades.Data : null;
        return View();
    }

    // GET /Course/Report/5 — báo cáo tiến độ
    public async Task<IActionResult> Report(int id)
    {
        var result = await _svc.GetProgressReportAsync(SchoolId, id);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }
        ViewBag.Report = result.Data;
        return View();
    }

    // ══ Course meta ═══════════════════════════════════════════════════════════

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> Create(CreateCourseRequest request)
    {
        var result = await _svc.CreateCourseAsync(SchoolId, UserId, request);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }

        TempData["Success"] = "Đã tạo khóa học — thêm chương và bài học để bắt đầu.";
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseRequest request) =>
        JsonResult(await _svc.UpdateCourseAsync(SchoolId, id, request));

    public sealed record ChangeStatusRequest(CourseStatus Status);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request) =>
        JsonResult(await _svc.ChangeCourseStatusAsync(SchoolId, id, request.Status));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _svc.DeleteCourseAsync(SchoolId, id);
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Detail), new { id }); }

        TempData["Success"] = "Đã xóa khóa học.";
        return RedirectToAction(nameof(Index));
    }

    // ══ Module & Lesson (JSON — course builder) ═══════════════════════════════

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> SaveModule([FromBody] SaveModuleRequest request) =>
        JsonResult(await _svc.SaveModuleAsync(SchoolId, request));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> DeleteModule(int id) =>
        JsonResult(await _svc.DeleteModuleAsync(SchoolId, id));

    public sealed record ReorderRequest(int ParentId, List<int> OrderedIds);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> ReorderModules([FromBody] ReorderRequest request) =>
        JsonResult(await _svc.ReorderModulesAsync(SchoolId, request.ParentId, request.OrderedIds));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> SaveLesson([FromBody] SaveLessonRequest request) =>
        JsonResult(await _svc.SaveLessonAsync(SchoolId, request));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> DeleteLesson(int id) =>
        JsonResult(await _svc.DeleteLessonAsync(SchoolId, id));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> ReorderLessons([FromBody] ReorderRequest request) =>
        JsonResult(await _svc.ReorderLessonsAsync(SchoolId, request.ParentId, request.OrderedIds));

    // ══ Gán lớp & ghi danh (JSON) ═════════════════════════════════════════════

    // GET /Course/Assignments/5 — refresh panel gán lớp
    [HttpGet]
    public async Task<IActionResult> Assignments(int id) =>
        JsonResult(await _svc.GetClassAssignmentsAsync(SchoolId, id));

    public sealed record AssignClassRequest(int CourseId, int ClassId);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> AssignClass([FromBody] AssignClassRequest request)
    {
        var result = await _svc.AssignToClassAsync(SchoolId, request.CourseId, request.ClassId, UserId);
        return JsonResult(result);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> SyncAssignment(int id) =>
        JsonResult(await _svc.SyncClassEnrollmentsAsync(SchoolId, id));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> DeactivateAssignment(int id) =>
        JsonResult(await _svc.DeactivateClassAssignmentAsync(SchoolId, id));

    public sealed record EnrollStudentRequest(int CourseId, int StudentId);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "CourseAuthoring")]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentRequest request) =>
        JsonResult(await _svc.EnrollStudentAsync(SchoolId, request.CourseId, request.StudentId, CourseEnrollmentSource.Manual));
}
