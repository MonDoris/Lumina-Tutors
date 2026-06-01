using System.Security.Claims;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers.Api;

/// <summary>
/// REST API cho mobile app — tất cả endpoints dùng JWT Bearer.
/// Route: /api/mobile/...
/// </summary>
[ApiController]
[Route("api/mobile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class MobileApiController : ControllerBase
{
    private readonly IGradingService      _grading;
    private readonly IAttendanceService   _attendance;
    private readonly IClassService        _classService;
    private readonly INotificationService _notifications;
    private readonly IDisciplineService   _discipline;
    private readonly IStudentService      _studentService;
    private readonly IHomeworkService     _homework;

    public MobileApiController(
        IGradingService      grading,
        IAttendanceService   attendance,
        IClassService        classService,
        INotificationService notifications,
        IDisciplineService   discipline,
        IStudentService      studentService,
        IHomeworkService     homework)
    {
        _grading        = grading;
        _attendance     = attendance;
        _classService   = classService;
        _notifications  = notifications;
        _discipline     = discipline;
        _studentService = studentService;
        _homework       = homework;
    }

    // ══════════════════════════════════════════════════════
    // STUDENT
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/student/grades?semesterId=1</summary>
    [HttpGet("student/grades")]
    public async Task<IActionResult> StudentGrades([FromQuery] int semesterId)
    {
        var result = await _grading.GetStudentSemesterSummaryAsync(UserId(), semesterId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/student/attendance?semesterId=1</summary>
    [HttpGet("student/attendance")]
    public async Task<IActionResult> StudentAttendance([FromQuery] int semesterId)
    {
        var result = await _attendance.GetStudentSummaryAsync(UserId(), semesterId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══════════════════════════════════════════════════════
    // TEACHER
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/teacher/classes?academicYearId=1</summary>
    [HttpGet("teacher/classes")]
    public async Task<IActionResult> TeacherClasses([FromQuery] int academicYearId)
    {
        var result = await _classService.GetAllAsync(SchoolId(), academicYearId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/subject-assignments?academicYearId=1 — các lớp-môn giáo viên phụ trách</summary>
    [HttpGet("teacher/subject-assignments")]
    public async Task<IActionResult> TeacherSubjectAssignments()
    {
        var result = await _homework.GetTeacherSubjectAssignmentsAsync(SchoolId(), UserId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/gradebook/{id}</summary>
    [HttpGet("teacher/gradebook/{subjectAssignmentId:int}")]
    public async Task<IActionResult> TeacherGradeBook(int subjectAssignmentId)
    {
        var result = await _grading.GetSubjectGradeBookAsync(subjectAssignmentId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/attendance-sessions?classId=1</summary>
    [HttpGet("teacher/attendance-sessions")]
    public async Task<IActionResult> AttendanceSessions([FromQuery] int classId, [FromQuery] DateOnly? date)
    {
        var d      = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _attendance.GetDailyReportAsync(classId, d);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/teacher/attendance-sessions</summary>
    [HttpPost("teacher/attendance-sessions")]
    public async Task<IActionResult> CreateAttendanceSession([FromBody] CreateSessionRequest model)
    {
        var result = await _attendance.CreateSessionAsync(SchoolId(), UserId(), model);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══════════════════════════════════════════════════════
    // PARENT
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/parent/children — danh sách con em liên kết với phụ huynh hiện tại</summary>
    [HttpGet("parent/children")]
    public async Task<IActionResult> ParentChildren()
    {
        var result = await _studentService.GetChildrenOfParentAsync(UserId(), SchoolId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/parent/child-grades?studentId=5&semesterId=1</summary>
    [HttpGet("parent/child-grades")]
    public async Task<IActionResult> ChildGrades([FromQuery] int studentId, [FromQuery] int semesterId)
    {
        var result = await _grading.GetStudentSemesterSummaryAsync(studentId, semesterId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/parent/child-attendance?studentId=5&semesterId=1</summary>
    [HttpGet("parent/child-attendance")]
    public async Task<IActionResult> ChildAttendance([FromQuery] int studentId, [FromQuery] int semesterId)
    {
        var result = await _attendance.GetStudentSummaryAsync(studentId, semesterId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══════════════════════════════════════════════════════
    // SUPERVISOR
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/supervisor/discipline?studentId=5</summary>
    [HttpGet("supervisor/discipline")]
    public async Task<IActionResult> DisciplineRecords(
        [FromQuery] int? studentId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1)
    {
        var result = await _discipline.GetRecordsAsync(
            SchoolId(), studentId, null, from, to, page, 20);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/supervisor/daily-report?date=2026-01-15</summary>
    [HttpGet("supervisor/daily-report")]
    public async Task<IActionResult> DailyReport([FromQuery] DateOnly? date)
    {
        var d      = date ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _discipline.GetDailyReportAsync(SchoolId(), d);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══════════════════════════════════════════════════════
    // COMMON
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/notifications?page=1</summary>
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery] int page = 1)
    {
        var result = await _notifications.GetForUserAsync(UserId(), page, 20);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/semesters</summary>
    [HttpGet("semesters")]
    public async Task<IActionResult> Semesters()
    {
        var result = await _classService.GetSemestersAsync(SchoolId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/academic-years</summary>
    [HttpGet("academic-years")]
    public async Task<IActionResult> AcademicYears()
    {
        var result = await _classService.GetAcademicYearsAsync(SchoolId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int UserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
