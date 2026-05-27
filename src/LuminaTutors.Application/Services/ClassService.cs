using AutoMapper;
using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class ClassService : IClassService
{
    private readonly IUnitOfWork        _uow;
    private readonly IMapper            _mapper;
    private readonly ILogger<ClassService> _logger;

    public ClassService(IUnitOfWork uow, IMapper mapper, ILogger<ClassService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── GetAll ───────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ClassSummaryDto>>> GetAllAsync(
        int schoolId, int academicYearId, CancellationToken ct = default)
    {
        var classes = await _uow.Classes.FindAsync(
            c => c.SchoolId == schoolId && c.AcademicYearId == academicYearId,
            include: q => q.Include(c => c.HomeRoomTeacher!)
                           .Include(c => c.Enrollments),
            ct: ct);

        var dtos = _mapper.Map<List<ClassSummaryDto>>(classes);
        return Result<IReadOnlyList<ClassSummaryDto>>.Success(dtos);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    public async Task<Result<ClassDetailDto>> GetByIdAsync(
        int schoolId, int classId, CancellationToken ct = default)
    {
        var classes = await _uow.Classes.FindAsync(
            c => c.Id == classId && c.SchoolId == schoolId,
            include: q => q
                .Include(c => c.HomeRoomTeacher!)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Subject)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Teacher)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Schedules)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student),
            ct: ct);

        var cls = classes.FirstOrDefault();
        if (cls is null)
            return Result<ClassDetailDto>.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        var dto = _mapper.Map<ClassDetailDto>(cls);
        return Result<ClassDetailDto>.Success(dto);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    public async Task<Result<ClassDetailDto>> CreateAsync(
        int schoolId, CreateClassRequest request, CancellationToken ct = default)
    {
        var duplicate = await _uow.Classes.FindAsync(
            c => c.SchoolId == schoolId &&
                 c.AcademicYearId == request.AcademicYearId &&
                 c.ClassName == request.ClassName.Trim(),
            ct: ct);

        if (duplicate.Any())
            return Result<ClassDetailDto>.Failure("DUPLICATE", "Tên lớp đã tồn tại trong năm học này.");

        var newClass = new Class
        {
            SchoolId           = schoolId,
            AcademicYearId     = request.AcademicYearId,
            GradeLevelId       = request.GradeLevelId,
            ClassName          = request.ClassName.Trim(),
            MaxStudents        = request.MaxStudents,
            HomeRoomTeacherId  = request.HomeRoomTeacherId,
            RoomNumber         = request.RoomNumber?.Trim()
        };

        await _uow.Classes.AddAsync(newClass, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Created class {ClassName} (Id={Id}) in school {SchoolId}",
            newClass.ClassName, newClass.Id, schoolId);

        return await GetByIdAsync(schoolId, newClass.Id, ct);
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    public async Task<Result<ClassDetailDto>> UpdateAsync(
        int schoolId, int classId, UpdateClassRequest request, CancellationToken ct = default)
    {
        var classes = await _uow.Classes.FindAsync(
            c => c.Id == classId && c.SchoolId == schoolId,
            ct: ct);

        var cls = classes.FirstOrDefault();
        if (cls is null)
            return Result<ClassDetailDto>.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        cls.ClassName          = request.ClassName.Trim();
        cls.MaxStudents        = request.MaxStudents;
        cls.HomeRoomTeacherId  = request.HomeRoomTeacherId;
        cls.RoomNumber         = request.RoomNumber?.Trim();

        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, classId, ct);
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    public async Task<Result> DeleteAsync(int schoolId, int classId, CancellationToken ct = default)
    {
        var classes = await _uow.Classes.FindAsync(
            c => c.Id == classId && c.SchoolId == schoolId,
            include: q => q.Include(c => c.Enrollments),
            ct: ct);

        var cls = classes.FirstOrDefault();
        if (cls is null)
            return Result.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        var hasActiveStudents = cls.Enrollments.Any(e => e.Status == Domain.Enums.EnrollmentStatus.Active);
        if (hasActiveStudents)
            return Result.Failure("HAS_STUDENTS", "Không thể xóa lớp đang có học sinh.");

        _uow.Classes.Remove(cls);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── AssignSubject ────────────────────────────────────────────────────────

    public async Task<Result> AssignSubjectAsync(
        int schoolId, int classId, AssignSubjectRequest request, CancellationToken ct = default)
    {
        var cls = await _uow.Classes.GetByIdAsync(classId, ct);
        if (cls is null || cls.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        var duplicate = await _uow.SubjectAssignments.FindAsync(
            sa => sa.ClassId    == classId &&
                  sa.SubjectId  == request.SubjectId &&
                  sa.SemesterId == request.SemesterId,
            ct: ct);

        if (duplicate.Any())
            return Result.Failure("DUPLICATE", "Môn học đã được phân công cho lớp này trong học kỳ.");

        var assignment = new SubjectAssignment
        {
            SchoolId       = schoolId,
            SemesterId     = request.SemesterId,
            ClassId        = classId,
            SubjectId      = request.SubjectId,
            TeacherId      = request.TeacherId,
            PeriodsPerWeek = request.PeriodsPerWeek
        };

        await _uow.SubjectAssignments.AddAsync(assignment, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── CreateSchedule ───────────────────────────────────────────────────────

    public async Task<Result> CreateScheduleAsync(
        int schoolId, int classId, CreateScheduleRequest request, CancellationToken ct = default)
    {
        var assignment = await _uow.SubjectAssignments.GetByIdAsync(request.SubjectAssignmentId, ct);
        if (assignment is null || assignment.SchoolId != schoolId || assignment.ClassId != classId)
            return Result.Failure("NOT_FOUND", "Phân công môn học không tồn tại.");

        var hasConflict = await HasScheduleConflictAsync(
            schoolId, assignment.SemesterId, assignment.TeacherId,
            request.DayOfWeek, request.PeriodStart, request.PeriodEnd,
            ct: ct);

        if (hasConflict)
            return Result.Failure("SCHEDULE_CONFLICT", "Giáo viên đã có lịch dạy trong khung giờ này.");

        var schedule = new Schedule
        {
            SchoolId            = schoolId,
            SemesterId          = assignment.SemesterId,
            SubjectAssignmentId = request.SubjectAssignmentId,
            DayOfWeek           = request.DayOfWeek,
            PeriodStart         = request.PeriodStart,
            PeriodEnd           = request.PeriodEnd,
            StartTime           = request.StartTime,
            EndTime             = request.EndTime,
            RoomOverride        = request.RoomOverride?.Trim()
        };

        await _uow.Schedules.AddAsync(schedule, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── GetAvailableTeachers ─────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<TeacherSummaryDto>>> GetAvailableTeachersAsync(
        int schoolId, CancellationToken ct = default)
    {
        var teachers = await _uow.TeacherProfiles.FindAsync(
            tp => tp.SchoolId == schoolId && tp.User.IsActive,
            include: q => q.Include(tp => tp.User),
            ct: ct);

        var dtos = _mapper.Map<List<TeacherSummaryDto>>(teachers);
        return Result<IReadOnlyList<TeacherSummaryDto>>.Success(dtos);
    }

    // ─── HasScheduleConflict ──────────────────────────────────────────────────

    public async Task<bool> HasScheduleConflictAsync(
        int schoolId, int semesterId, int teacherId,
        byte dayOfWeek, byte periodStart, byte periodEnd,
        int? excludeScheduleId = null,
        CancellationToken ct = default)
    {
        var conflicts = await _uow.Schedules.FindAsync(
            s => s.SubjectAssignment.SemesterId == semesterId &&
                 s.SubjectAssignment.TeacherId  == teacherId  &&
                 s.DayOfWeek == dayOfWeek                      &&
                 s.PeriodStart <= periodEnd                    &&
                 s.PeriodEnd   >= periodStart                  &&
                 (!excludeScheduleId.HasValue || s.Id != excludeScheduleId.Value),
            include: q => q.Include(s => s.SubjectAssignment),
            ct: ct);

        return conflicts.Any();
    }
}
