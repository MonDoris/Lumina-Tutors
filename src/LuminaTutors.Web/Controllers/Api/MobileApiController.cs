using System.Security.Claims;
using LuminaTutors.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Application.DTOs.AI;
using LuminaTutors.Application.DTOs.Lab;
using LuminaTutors.Application.DTOs.OnlineClassroom;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly IGradingService           _grading;
    private readonly IAttendanceService        _attendance;
    private readonly IClassService             _classService;
    private readonly INotificationService      _notifications;
    private readonly IDisciplineService        _discipline;
    private readonly IStudentService           _studentService;
    private readonly IHomeworkService          _homework;
    private readonly IAiTutorService           _aiTutor;
    private readonly IOnlineClassroomService   _onlineClassroom;
    private readonly IVirtualLabService        _virtualLab;
    private readonly IMemoryCache              _cache;
    private readonly IUnitOfWork               _uow;

    public MobileApiController(
        IGradingService          grading,
        IAttendanceService       attendance,
        IClassService            classService,
        INotificationService     notifications,
        IDisciplineService       discipline,
        IStudentService          studentService,
        IHomeworkService         homework,
        IAiTutorService          aiTutor,
        IOnlineClassroomService  onlineClassroom,
        IVirtualLabService       virtualLab,
        IMemoryCache             cache,
        IUnitOfWork              uow)
    {
        _grading         = grading;
        _attendance      = attendance;
        _classService    = classService;
        _notifications   = notifications;
        _discipline      = discipline;
        _studentService  = studentService;
        _homework        = homework;
        _aiTutor         = aiTutor;
        _onlineClassroom = onlineClassroom;
        _virtualLab      = virtualLab;
        _cache           = cache;
        _uow             = uow;
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

    /// <summary>POST /api/mobile/student/scan-qr — quét QR điểm danh</summary>
    [HttpPost("student/scan-qr")]
    public async Task<IActionResult> ScanQR([FromBody] ScanQRRequest model)
    {
        var req    = model with { StudentId = UserId() };
        var result = await _attendance.ScanQRAsync(req);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/student/courses — các môn học (để xem bài tập)</summary>
    [HttpGet("student/courses")]
    public async Task<IActionResult> StudentCourses()
    {
        var result = await _homework.GetStudentCoursesAsync(SchoolId(), UserId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/student/homework/{saId} — bài tập theo môn</summary>
    [HttpGet("student/homework/{subjectAssignmentId:int}")]
    public async Task<IActionResult> StudentHomework(int subjectAssignmentId)
    {
        var result = await _homework.GetStudentAssignmentsAsync(SchoolId(), UserId(), subjectAssignmentId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/student/ai-tutor/sessions</summary>
    [HttpGet("student/ai-tutor/sessions")]
    public async Task<IActionResult> AiTutorSessions()
    {
        var result = await _aiTutor.GetStudentSessionsAsync(SchoolId(), UserId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/student/ai-tutor/sessions — tạo phiên mới</summary>
    [HttpPost("student/ai-tutor/sessions")]
    public async Task<IActionResult> CreateAiSession([FromBody] CreateAiSessionDto dto)
    {
        var result = await _aiTutor.CreateSessionAsync(SchoolId(), UserId(), dto.Title ?? "Câu hỏi mới");
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/student/ai-tutor/{sessionId}/messages</summary>
    [HttpGet("student/ai-tutor/{sessionId:int}/messages")]
    public async Task<IActionResult> AiTutorMessages(int sessionId)
    {
        var result = await _aiTutor.GetMessagesAsync(sessionId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/student/ai-tutor/{sessionId}/messages — gửi tin nhắn</summary>
    [HttpPost("student/ai-tutor/{sessionId:int}/messages")]
    public async Task<IActionResult> SendAiMessage(int sessionId, [FromBody] SendMessageRequest dto)
    {
        var result = await _aiTutor.SendMessageAsync(SchoolId(), sessionId, UserId(), dto.Content);
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

    /// <summary>PATCH /api/mobile/teacher/attendance-sessions/{sessionId}/record — cập nhật 1 bản ghi điểm danh</summary>
    [HttpPatch("teacher/attendance-sessions/{sessionId:int}/record")]
    public async Task<IActionResult> UpdateAttendanceRecord(int sessionId, [FromBody] UpdateAttendanceRequest model)
    {
        var result = await _attendance.UpdateAttendanceAsync(sessionId, UserId(), model);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/sessions/{sessionId} — lấy session + QR token</summary>
    [HttpGet("teacher/sessions/{sessionId:int}")]
    public async Task<IActionResult> GetAttendanceSession(int sessionId)
    {
        var result = await _attendance.GetSessionAsync(sessionId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/teacher/sessions/{sessionId}/notify — thông báo phụ huynh HS vắng</summary>
    [HttpPost("teacher/sessions/{sessionId:int}/notify")]
    public async Task<IActionResult> NotifyAbsent(int sessionId)
    {
        var result = await _attendance.NotifyAbsentParentsAsync(sessionId);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/homework — danh sách bài tập giáo viên đã tạo</summary>
    [HttpGet("teacher/homework")]
    public async Task<IActionResult> TeacherHomework()
    {
        var result = await _homework.GetTeacherAssignmentsAsync(SchoolId(), UserId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/teacher/schedules?subjectAssignmentId=1 — lịch học của môn</summary>
    [HttpGet("teacher/schedules")]
    public async Task<IActionResult> TeacherSchedules([FromQuery] int subjectAssignmentId)
    {
        var schedules = await _uow.Schedules.FindAsync(
            s => s.SubjectAssignmentId == subjectAssignmentId,
            ct: default);

        var result = schedules.Select(s => new
        {
            scheduleId     = s.Id,
            dayOfWeek      = s.DayOfWeek,
            dayName        = s.DayOfWeek switch { 2=>"Thứ 2",3=>"Thứ 3",4=>"Thứ 4",5=>"Thứ 5",6=>"Thứ 6",7=>"Thứ 7",_=>"CN" },
            periodStart    = s.PeriodStart,
            periodEnd      = s.PeriodEnd,
            startTime      = s.StartTime.ToString("HH:mm"),
            endTime        = s.EndTime.ToString("HH:mm"),
        }).ToList();

        return Ok(result);
    }

    /// <summary>GET /api/mobile/teacher/grade-categories — loại điểm theo trường</summary>
    [HttpGet("teacher/grade-categories")]
    public async Task<IActionResult> GradeCategories()
    {
        var cats = await _uow.GradeCategories.GetAllAsync(default);

        var result = cats.OrderBy(c => c.Id).Select(c => new
        {
            id           = c.Id,
            categoryCode = c.CategoryCode,
            categoryName = c.CategoryName,
            coefficient  = c.Coefficient,
        }).ToList();

        return Ok(result);
    }

    /// <summary>POST /api/mobile/teacher/enter-score — nhập điểm 1 học sinh</summary>
    [HttpPost("teacher/enter-score")]
    public async Task<IActionResult> EnterScore([FromBody] EnterScoreRequest model)
    {
        var result = await _grading.EnterScoreAsync(SchoolId(), UserId(), model);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/teacher/bulk-enter-scores — nhập điểm hàng loạt</summary>
    [HttpPost("teacher/bulk-enter-scores")]
    public async Task<IActionResult> BulkEnterScores([FromBody] BulkEnterScoreRequest model)
    {
        var result = await _grading.BulkEnterScoresAsync(SchoolId(), UserId(), model);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/teacher/calculate-averages/{id} — tính ĐTBm tất cả HS</summary>
    [HttpPost("teacher/calculate-averages/{subjectAssignmentId:int}")]
    public async Task<IActionResult> CalculateAverages(int subjectAssignmentId)
    {
        var result = await _grading.CalculateAllAveragesAsync(subjectAssignmentId);
        return result.IsSuccess ? Ok(new { updated = result.Data }) : BadRequest(new { message = result.Error });
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

    /// <summary>GET /api/mobile/parent/child-homework?studentId=5&saId=1 — bài tập của con</summary>
    [HttpGet("parent/child-homework")]
    public async Task<IActionResult> ChildHomework([FromQuery] int studentId, [FromQuery] int subjectAssignmentId)
    {
        var result = await _homework.GetStudentAssignmentsAsync(SchoolId(), studentId, subjectAssignmentId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/parent/child-courses?studentId=5 — các môn của con</summary>
    [HttpGet("parent/child-courses")]
    public async Task<IActionResult> ChildCourses([FromQuery] int studentId)
    {
        var result = await _homework.GetStudentCoursesAsync(SchoolId(), studentId);
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

    /// <summary>POST /api/mobile/supervisor/violations — ghi nhận vi phạm</summary>
    [HttpPost("supervisor/violations")]
    public async Task<IActionResult> CreateViolation([FromBody] CreateDisciplineRecordRequest model)
    {
        var result = await _discipline.CreateRecordAsync(SchoolId(), UserId(), model);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/supervisor/violations/{id}/resolve — xử lý vi phạm</summary>
    [HttpPost("supervisor/violations/{id:int}/resolve")]
    public async Task<IActionResult> ResolveViolation(int id, [FromBody] ResolveViolationDto model)
    {
        var result = await _discipline.ResolveRecordAsync(id, model.ActionTaken, UserId());
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/supervisor/violations/{id}/escalate — chuyển lên cấp trên</summary>
    [HttpPost("supervisor/violations/{id:int}/escalate")]
    public async Task<IActionResult> EscalateViolation(int id, [FromBody] EscalateDto dto)
    {
        var result = await _discipline.EscalateRecordAsync(id, dto.EscalateToUserId);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/supervisor/gate-check — ghi nhận kiểm tra cổng</summary>
    [HttpPost("supervisor/gate-check")]
    public async Task<IActionResult> GateCheck([FromBody] GateCheckDto dto)
    {
        var result = await _discipline.RecordGateCheckAsync(
            SchoolId(), dto.StudentId, dto.CheckType, UserId(), dto.IsLate, dto.Note);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/supervisor/students — danh sách học sinh của trường để chọn khi ghi vi phạm</summary>
    [HttpGet("supervisor/students")]
    public async Task<IActionResult> SchoolStudents()
    {
        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => sp.SchoolId == SchoolId(),
            include: q => q.Include(sp => sp.User),
            ct: default);

        var result = profiles
            .Where(sp => sp.User.IsActive)
            .OrderBy(sp => sp.User.FullName)
            .Select(sp => new
            {
                userId      = sp.UserId,
                fullName    = sp.User.FullName,
                studentCode = sp.StudentCode,
            }).ToList();

        return Ok(result);
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

    // ══════════════════════════════════════════════════════
    // ONLINE CLASSROOM
    // ══════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/online-sessions — danh sách phòng học online</summary>
    [HttpGet("online-sessions")]
    public async Task<IActionResult> OnlineSessions()
    {
        var result = await _onlineClassroom.GetSessionsAsync(SchoolId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>POST /api/mobile/online-sessions/join — tham gia bằng mã phòng</summary>
    [HttpPost("online-sessions/join")]
    public async Task<IActionResult> JoinOnlineSession([FromBody] JoinByCodeDto dto)
    {
        var result = await _onlineClassroom.JoinByCodeAsync(SchoolId(), UserId(), dto.RoomCode);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    /// <summary>GET /api/mobile/online-sessions/{id}/chat — lịch sử chat</summary>
    [HttpGet("online-sessions/{sessionId:int}/chat")]
    public async Task<IActionResult> OnlineChatHistory(int sessionId)
    {
        var result = await _onlineClassroom.GetChatHistoryAsync(sessionId);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══ Virtual Lab ══════════════════════════════════════════════════════════

    /// <summary>GET /api/mobile/virtual-lab/sessions — danh sách phòng lab đang hoạt động</summary>
    [HttpGet("virtual-lab/sessions")]
    public async Task<IActionResult> VirtualLabSessions()
    {
        var result = await _virtualLab.GetActiveSessionsAsync(SchoolId());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    // ══ WebView bridge token ═════════════════════════════════════════════════
    // Mobile đã xác thực qua JWT, tạo bridge code ngắn hạn để mở WebView
    // mà không cần truyền JWT dài qua URL (tránh lỗi encoding)

    /// <summary>POST /api/mobile/webview-token — tạo one-time bridge code (60s)</summary>
    [HttpPost("webview-token")]
    public IActionResult CreateWebViewToken()
    {
        // Collect all claims from current (JWT-authenticated) user
        var claims = User.Claims.Select(c => new WebViewBridgeClaim(c.Type, c.Value)).ToList();

        // Generate a short random code, store in cache for 90 seconds
        var code = Guid.NewGuid().ToString("N"); // 32 hex chars, no hyphens
        _cache.Set($"webview:{code}", claims, TimeSpan.FromSeconds(90));

        return Ok(new { code });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int UserId()   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId() => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}

public record ResolveViolationDto(string ActionTaken);
public record EscalateDto(int EscalateToUserId);
public record GateCheckDto(int StudentId, string CheckType, bool IsLate, string? Note);
public record JoinByCodeDto(string RoomCode);
public record CreateAiSessionDto(string? Title);
