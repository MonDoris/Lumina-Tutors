using AutoMapper;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork               _uow;
    private readonly IMapper                   _mapper;
    private readonly IConfiguration            _config;
    private readonly ILogger<AttendanceService> _logger;

    private int QRExpiryMinutes =>
        int.TryParse(_config["QrSettings:AttendanceTokenExpiryMinutes"], out var v) ? v : 10;

    public AttendanceService(
        IUnitOfWork uow,
        IMapper mapper,
        IConfiguration config,
        ILogger<AttendanceService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _config = config;
        _logger = logger;
    }

    // ─── CreateSession ────────────────────────────────────────────────────────

    public async Task<Result<AttendanceSessionDto>> CreateSessionAsync(
        int schoolId, int teacherId, CreateSessionRequest request, CancellationToken ct = default)
    {
        var existing = await _uow.AttendanceSessions.FindAsync(
            s => s.ScheduleId == request.ScheduleId &&
                 s.SessionDate  == request.SessionDate &&
                 s.SessionStatus != SessionStatus.Cancelled,
            ct: ct);

        if (existing.Any())
            return Result<AttendanceSessionDto>.Failure(
                "SESSION_EXISTS", "Đã có phiên điểm danh cho buổi học này.");

        var session = new AttendanceSession
        {
            SchoolId             = schoolId,
            ScheduleId           = request.ScheduleId,
            CreatedByTeacherId   = teacherId,
            SessionDate          = request.SessionDate,
            QRToken              = Guid.NewGuid(),
            QRExpiresAt          = DateTime.UtcNow.AddMinutes(QRExpiryMinutes),
            SessionStatus        = SessionStatus.Open,
            TopicNote            = request.TopicNote?.Trim()
        };

        await _uow.AttendanceSessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        // Pre-populate attendance records for all enrolled students
        var enrollment = await _uow.ClassEnrollments.FindAsync(
            e => e.Class.SubjectAssignments
                         .Any(sa => sa.Schedules.Any(sc => sc.Id == request.ScheduleId)) &&
                 e.Status == EnrollmentStatus.Active,
            include: q => q.Include(e => e.Class)
                           .ThenInclude(c => c.SubjectAssignments)
                           .ThenInclude(sa => sa.Schedules),
            ct: ct);

        var records = enrollment.Select(e => new StudentAttendance
        {
            SessionId      = session.Id,
            StudentId      = e.StudentId,
            Status         = AttendanceStatus.Absent,
            NotifiedParent = false
        }).ToList();

        if (records.Count > 0)
        {
            await _uow.StudentAttendances.AddRangeAsync(records, ct);
            await _uow.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "AttendanceSession {Id} opened by teacher {TeacherId} for schedule {ScheduleId} on {Date}",
            session.Id, teacherId, request.ScheduleId, request.SessionDate);

        return await GetSessionAsync(session.Id, ct);
    }

    // ─── GetSession ───────────────────────────────────────────────────────────

    public async Task<Result<AttendanceSessionDto>> GetSessionAsync(
        int sessionId, CancellationToken ct = default)
    {
        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.Id == sessionId,
            include: q => q
                .Include(s => s.Schedule)
                    .ThenInclude(sc => sc.SubjectAssignment).ThenInclude(sa => sa.Subject)
                .Include(s => s.CreatedByTeacher)
                .Include(s => s.Attendances).ThenInclude(sa => sa.Student),
            ct: ct);

        var session = sessions.FirstOrDefault();
        if (session is null)
            return Result<AttendanceSessionDto>.Failure("NOT_FOUND", "Phiên điểm danh không tồn tại.");

        var dto     = _mapper.Map<AttendanceSessionDto>(session);
        var records = _mapper.Map<List<AttendanceRecordDto>>(session.Attendances?.ToList() ?? new());
        dto = dto with { Records = records };
        return Result<AttendanceSessionDto>.Success(dto);
    }

    // ─── GetActiveSession ─────────────────────────────────────────────────────

    public async Task<Result<AttendanceSessionDto>> GetActiveSessionAsync(
        int scheduleId, DateOnly date, CancellationToken ct = default)
    {
        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.ScheduleId    == scheduleId &&
                 s.SessionDate   == date        &&
                 s.SessionStatus == SessionStatus.Open,
            ct: ct);

        var session = sessions.FirstOrDefault();
        if (session is null)
            return Result<AttendanceSessionDto>.Failure("NOT_FOUND", "Không có phiên điểm danh đang mở.");

        return await GetSessionAsync(session.Id, ct);
    }

    // ─── CloseSession ─────────────────────────────────────────────────────────

    public async Task<Result> CloseSessionAsync(
        int sessionId, int teacherId, CancellationToken ct = default)
    {
        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.Id == sessionId && s.CreatedByTeacherId == teacherId,
            ct: ct);

        var session = sessions.FirstOrDefault();
        if (session is null)
            return Result.Failure("NOT_FOUND", "Phiên điểm danh không tồn tại hoặc bạn không có quyền.");

        if (session.SessionStatus != SessionStatus.Open)
            return Result.Failure("ALREADY_CLOSED", "Phiên điểm danh đã đóng.");

        session.SessionStatus = SessionStatus.Closed;
        session.ClosedAt      = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("AttendanceSession {Id} closed by teacher {TeacherId}", sessionId, teacherId);
        return Result.Success();
    }

    // ─── ScanQR (student side) ────────────────────────────────────────────────

    public async Task<Result<ScanQRResponse>> ScanQRAsync(
        ScanQRRequest request, CancellationToken ct = default)
    {
        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.QRToken == request.QRToken && s.SessionStatus == SessionStatus.Open,
            include: q => q.Include(s => s.Schedule)
                           .ThenInclude(sc => sc.SubjectAssignment)
                           .ThenInclude(sa => sa.Subject)
                           .Include(s => s.Schedule)
                           .ThenInclude(sc => sc.SubjectAssignment)
                           .ThenInclude(sa => sa.Class),
            ct: ct);

        var session = sessions.FirstOrDefault();
        if (session is null)
            return Result<ScanQRResponse>.Failure("QR_INVALID", "Mã QR không hợp lệ hoặc phiên đã đóng.");

        if (DateTime.UtcNow > session.QRExpiresAt)
            return Result<ScanQRResponse>.Failure("QR_EXPIRED", "Mã QR đã hết hạn. Vui lòng liên hệ giáo viên.");

        var attendance = await _uow.StudentAttendances.FindAsync(
            sa => sa.SessionId == session.Id && sa.StudentId == request.StudentId,
            include: q => q.Include(sa => sa.Student),
            ct: ct);

        var record = attendance.FirstOrDefault();
        if (record is null)
            return Result<ScanQRResponse>.Failure("NOT_ENROLLED", "Học sinh không thuộc lớp này.");

        var studentName  = record.Student?.FullName ?? string.Empty;
        var subjectName  = session.Schedule?.SubjectAssignment?.Subject?.SubjectName ?? string.Empty;
        var className    = session.Schedule?.SubjectAssignment?.Class?.ClassName ?? string.Empty;

        if (record.Status == AttendanceStatus.Present)
            return Result<ScanQRResponse>.Success(new ScanQRResponse(
                Success:      true,
                Message:      "Bạn đã điểm danh thành công trước đó.",
                StudentName:  studentName,
                SubjectName:  subjectName,
                ClassName:    className,
                CheckedInAt:  record.CheckedInAt ?? DateTime.UtcNow));

        record.Status      = AttendanceStatus.Present;
        record.CheckMethod = CheckMethod.QrScan;
        record.CheckedInAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Student {StudentId} scanned QR for session {SessionId}", request.StudentId, session.Id);

        return Result<ScanQRResponse>.Success(new ScanQRResponse(
            Success:     true,
            Message:     "Điểm danh thành công!",
            StudentName: studentName,
            SubjectName: subjectName,
            ClassName:   className,
            CheckedInAt: record.CheckedInAt!.Value));
    }

    // ─── UpdateAttendance (teacher override) ──────────────────────────────────

    public async Task<Result> UpdateAttendanceAsync(
        int sessionId, int teacherId, UpdateAttendanceRequest request, CancellationToken ct = default)
    {
        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.Id == sessionId && s.CreatedByTeacherId == teacherId,
            ct: ct);

        if (!sessions.Any())
            return Result.Failure("NOT_FOUND", "Phiên điểm danh không tồn tại hoặc bạn không có quyền.");

        var attendance = await _uow.StudentAttendances.FindAsync(
            sa => sa.SessionId == sessionId && sa.StudentId == request.StudentId,
            ct: ct);

        var record = attendance.FirstOrDefault();
        if (record is null)
            return Result.Failure("NOT_FOUND", "Không tìm thấy bản ghi điểm danh.");

        record.Status             = request.Status;
        record.CheckMethod        = CheckMethod.Manual;
        record.UpdatedByTeacherId = teacherId;
        record.Note               = request.Note?.Trim();

        if (request.Status == AttendanceStatus.Present && record.CheckedInAt is null)
            record.CheckedInAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── GetSessionRecords ────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<AttendanceRecordDto>>> GetSessionRecordsAsync(
        int sessionId, CancellationToken ct = default)
    {
        var records = await _uow.StudentAttendances.FindAsync(
            sa => sa.SessionId == sessionId,
            include: q => q.Include(sa => sa.Student),
            ct: ct);

        var dtos = _mapper.Map<List<AttendanceRecordDto>>(records);
        return Result<IReadOnlyList<AttendanceRecordDto>>.Success(dtos);
    }

    // ─── GetDailyReport ───────────────────────────────────────────────────────

    public async Task<Result<DailyAttendanceReportDto>> GetDailyReportAsync(
        int classId, DateOnly date, CancellationToken ct = default)
    {
        var classEntity = await _uow.Classes.GetByIdAsync(classId, ct);
        var className   = classEntity?.ClassName ?? string.Empty;

        var sessions = await _uow.AttendanceSessions.FindAsync(
            s => s.Schedule.SubjectAssignment.ClassId == classId && s.SessionDate == date,
            include: q => q
                .Include(s => s.Schedule).ThenInclude(sc => sc.SubjectAssignment).ThenInclude(sa => sa.Subject)
                .Include(s => s.Attendances).ThenInclude(sa => sa.Student),
            ct: ct);

        var allRecords = sessions.SelectMany(s => s.Attendances).ToList();

        var total     = allRecords.Select(r => r.StudentId).Distinct().Count();
        var present   = allRecords.Count(r => r.Status == AttendanceStatus.Present);
        var absent    = allRecords.Count(r => r.Status == AttendanceStatus.Absent);
        var late      = allRecords.Count(r => r.Status == AttendanceStatus.Late);
        var excused   = allRecords.Count(r => r.Status == AttendanceStatus.Excused);
        var rate      = total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0m;

        var dto = new DailyAttendanceReportDto(
            ReportDate:     date,
            ClassName:      className,
            TotalStudents:  total,
            PresentCount:   present,
            AbsentCount:    absent,
            LateCount:      late,
            ExcusedCount:   excused,
            AttendanceRate: rate,
            Records:        _mapper.Map<List<AttendanceRecordDto>>(allRecords));

        return Result<DailyAttendanceReportDto>.Success(dto);
    }

    // ─── GetStudentSummary ────────────────────────────────────────────────────

    public async Task<Result<StudentAttendanceSummaryDto>> GetStudentSummaryAsync(
        int studentId, int semesterId, CancellationToken ct = default)
    {
        var student  = await _uow.Users.GetByIdAsync(studentId, ct);
        var semester = await _uow.Semesters.GetByIdAsync(semesterId, ct);

        var records = await _uow.StudentAttendances.FindAsync(
            sa => sa.StudentId == studentId &&
                  sa.Session.Schedule.SubjectAssignment.SemesterId == semesterId,
            include: q => q.Include(sa => sa.Session)
                           .ThenInclude(s => s.Schedule)
                           .ThenInclude(sc => sc.SubjectAssignment)
                           .ThenInclude(sa2 => sa2.Subject),
            ct: ct);

        var total   = records.Count;
        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent  = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late    = records.Count(r => r.Status == AttendanceStatus.Late);
        var excused = records.Count(r => r.Status == AttendanceStatus.Excused);
        var rate    = total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0m;

        var absenceDates = records
            .Where(r => r.Status == AttendanceStatus.Absent || r.Status == AttendanceStatus.Late)
            .Select(r => new AbsenceDateDto(
                SessionDate: r.Session.SessionDate,
                SubjectName: r.Session.Schedule?.SubjectAssignment?.Subject?.SubjectName ?? string.Empty,
                Status:      r.Status.ToString()))
            .ToList();

        var dto = new StudentAttendanceSummaryDto(
            StudentId:      studentId,
            StudentName:    student?.FullName ?? string.Empty,
            SemesterId:     semesterId,
            SemesterName:   semester?.SemesterName ?? string.Empty,
            TotalSessions:  total,
            PresentCount:   present,
            AbsentCount:    absent,
            LateCount:      late,
            ExcusedCount:   excused,
            AttendanceRate: rate,
            AbsenceDates:   absenceDates);

        return Result<StudentAttendanceSummaryDto>.Success(dto);
    }

    // ─── NotifyAbsentParents ──────────────────────────────────────────────────

    public async Task<Result<int>> NotifyAbsentParentsAsync(
        int sessionId, CancellationToken ct = default)
    {
        await _uow.ExecuteStoredProcedureAsync(
            "SP_NotifyAbsentStudents",
            new { SessionId = sessionId },
            ct);

        _logger.LogInformation("NotifyAbsentParents: SP_NotifyAbsentStudents executed for session {Id}", sessionId);

        var absentRecords = await _uow.StudentAttendances.FindAsync(
            sa => sa.SessionId == sessionId &&
                  sa.Status == AttendanceStatus.Absent &&
                  !sa.NotifiedParent,
            ct: ct);

        foreach (var r in absentRecords)
            r.NotifiedParent = true;

        await _uow.SaveChangesAsync(ct);

        return Result<int>.Success(absentRecords.Count);
    }
}
