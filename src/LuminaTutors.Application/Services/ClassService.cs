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
                .Include(c => c.GradeLevel)
                .Include(c => c.AcademicYear)
                .Include(c => c.HomeRoomTeacher!)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Subject)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Teacher)
                .Include(c => c.SubjectAssignments)
                    .ThenInclude(sa => sa.Schedules)
                .Include(c => c.Enrollments.Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active))
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.StudentProfile),
            ct: ct);

        var cls = classes.FirstOrDefault();
        if (cls is null)
            return Result<ClassDetailDto>.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        return Result<ClassDetailDto>.Success(BuildClassDetailDto(cls));
    }

    private static ClassDetailDto BuildClassDetailDto(Class cls)
    {
        var subjects = cls.SubjectAssignments.Select(sa => new SubjectAssignmentDto(
            sa.Id,
            sa.Subject?.SubjectName ?? "",
            sa.Subject?.SubjectCode ?? "",
            sa.Teacher?.FullName    ?? "",
            sa.PeriodsPerWeek
        )).ToList();

        var schedule = cls.SubjectAssignments
            .SelectMany(sa => sa.Schedules.Select(s => new ScheduleSlotDto(
                s.Id,
                sa.Subject?.SubjectName ?? "",
                sa.Teacher?.FullName    ?? "",
                s.DayOfWeek,
                GetDayName(s.DayOfWeek),
                s.PeriodStart,
                s.PeriodEnd,
                s.StartTime.ToString("HH:mm"),
                s.EndTime.ToString("HH:mm"),
                s.RoomOverride
            ))).ToList();

        var students = cls.Enrollments
            .Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active)
            .Select(e => new ClassStudentDto(
                e.Student.Id,
                e.Student.FullName,
                e.Student.StudentProfile?.StudentCode ?? "",
                e.Student.PhoneNumber,
                e.Student.AvatarUrl,
                e.EnrolledDate
            )).OrderBy(s => s.FullName).ToList();

        return new ClassDetailDto(
            ClassId:            cls.Id,
            ClassName:          cls.ClassName,
            GradeLevelId:       cls.GradeLevelId,
            GradeName:          cls.GradeLevel?.GradeName ?? "",
            EducationLevel:     cls.GradeLevel?.EducationLevel.ToString() ?? "",
            AcademicYearId:     cls.AcademicYearId,
            AcademicYearName:   cls.AcademicYear?.YearName ?? "",
            HomeRoomTeacherId:  cls.HomeRoomTeacherId,
            HomeRoomTeacherName:cls.HomeRoomTeacher?.FullName,
            RoomNumber:         cls.RoomNumber,
            MaxStudents:        cls.MaxStudents,
            IsActive:           cls.IsActive,
            SubjectAssignments: subjects,
            Schedule:           schedule,
            Students:           students
        );
    }

    private static string GetDayName(byte day) => day switch
    {
        2 => "Thứ Hai", 3 => "Thứ Ba", 4 => "Thứ Tư",
        5 => "Thứ Năm", 6 => "Thứ Sáu", 7 => "Thứ Bảy",
        _ => "Chủ Nhật"
    };

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

    // ─── EnrollStudent ────────────────────────────────────────────────────────

    public async Task<Result> EnrollStudentAsync(
        int schoolId, int classId, int studentUserId, CancellationToken ct = default)
    {
        // Verify class belongs to school
        var cls = await _uow.Classes.GetByIdAsync(classId, ct);
        if (cls is null || cls.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        // Verify student belongs to school
        var students = await _uow.Users.FindAsync(
            u => u.Id == studentUserId && u.SchoolId == schoolId,
            include: q => q.Include(u => u.Role),
            ct: ct);
        var student = students.FirstOrDefault();
        if (student is null || student.Role.RoleCode != "STUDENT")
            return Result.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        // Check already enrolled in THIS class
        var alreadyIn = await _uow.ClassEnrollments.FindAsync(
            e => e.ClassId == classId && e.StudentId == studentUserId &&
                 e.Status == Domain.Enums.EnrollmentStatus.Active, ct: ct);
        if (alreadyIn.Any())
            return Result.Failure("ALREADY_ENROLLED", "Học sinh đã được xếp vào lớp này.");

        // Withdraw from any other active class first
        var existing = await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == studentUserId &&
                 e.Status == Domain.Enums.EnrollmentStatus.Active, ct: ct);
        foreach (var e in existing) e.Status = Domain.Enums.EnrollmentStatus.Withdrawn;

        await _uow.ClassEnrollments.AddAsync(new Domain.Entities.Academic.ClassEnrollment
        {
            ClassId      = classId,
            StudentId    = studentUserId,
            EnrolledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status       = Domain.Enums.EnrollmentStatus.Active
        }, ct);

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Student UserId={StudentId} enrolled in Class {ClassId}", studentUserId, classId);
        return Result.Success();
    }

    // ─── RemoveStudent ────────────────────────────────────────────────────────

    public async Task<Result> RemoveStudentAsync(
        int schoolId, int classId, int studentUserId, CancellationToken ct = default)
    {
        var cls = await _uow.Classes.GetByIdAsync(classId, ct);
        if (cls is null || cls.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        var enrollments = await _uow.ClassEnrollments.FindAsync(
            e => e.ClassId == classId && e.StudentId == studentUserId &&
                 e.Status == Domain.Enums.EnrollmentStatus.Active, ct: ct);
        var enrollment = enrollments.FirstOrDefault();
        if (enrollment is null)
            return Result.Failure("NOT_ENROLLED", "Học sinh không có trong lớp này.");

        enrollment.Status = Domain.Enums.EnrollmentStatus.Withdrawn;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Student UserId={StudentId} removed from Class {ClassId}", studentUserId, classId);
        return Result.Success();
    }

    // ─── GetUnassignedStudents ────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ClassStudentDto>>> GetUnassignedStudentsAsync(
        int schoolId, int classId, CancellationToken ct = default)
    {
        // Students in school NOT currently enrolled in classId
        var allStudents = await _uow.StudentProfiles.FindAsync(
            sp => sp.SchoolId == schoolId && sp.User.IsActive,
            include: q => q.Include(sp => sp.User),
            ct: ct);

        // Get IDs already in this class
        var enrolledIds = (await _uow.ClassEnrollments.FindAsync(
            e => e.ClassId == classId && e.Status == Domain.Enums.EnrollmentStatus.Active, ct: ct))
            .Select(e => e.StudentId)
            .ToHashSet();

        var unassigned = allStudents
            .Where(sp => !enrolledIds.Contains(sp.UserId))
            .OrderBy(sp => sp.User.FullName)
            .Select(sp => new ClassStudentDto(
                sp.UserId,
                sp.User.FullName,
                sp.StudentCode,
                sp.User.PhoneNumber,
                sp.User.AvatarUrl,
                DateOnly.MinValue // not enrolled yet
            ))
            .ToList() as IReadOnlyList<ClassStudentDto>;

        return Result<IReadOnlyList<ClassStudentDto>>.Success(unassigned);
    }

    // ─── GetGradeLevels ───────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<GradeLevelSelectDto>>> GetGradeLevelsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var levels = await _uow.GradeLevels.FindAsync(
            gl => gl.SchoolId == schoolId, ct: ct);

        var dtos = levels
            .OrderBy(gl => gl.GradeNumber)
            .Select(gl => new GradeLevelSelectDto(gl.Id, gl.GradeName, gl.GradeNumber))
            .ToList() as IReadOnlyList<GradeLevelSelectDto>;

        return Result<IReadOnlyList<GradeLevelSelectDto>>.Success(dtos);
    }

    // ─── GetAcademicYears ─────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<AcademicYearSelectDto>>> GetAcademicYearsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var years = await _uow.AcademicYears.FindAsync(
            ay => ay.SchoolId == schoolId, ct: ct);

        var dtos = years
            .OrderByDescending(ay => ay.StartDate)
            .Select(ay => new AcademicYearSelectDto(ay.Id, ay.YearName, ay.IsActive))
            .ToList() as IReadOnlyList<AcademicYearSelectDto>;

        return Result<IReadOnlyList<AcademicYearSelectDto>>.Success(dtos);
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
