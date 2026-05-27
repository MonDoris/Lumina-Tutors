using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IClassService
{
    Task<Result<IReadOnlyList<ClassSummaryDto>>>   GetAllAsync(int schoolId, int academicYearId, CancellationToken ct = default);
    Task<Result<ClassDetailDto>>                   GetByIdAsync(int schoolId, int classId, CancellationToken ct = default);
    Task<Result<ClassDetailDto>>                   CreateAsync(int schoolId, CreateClassRequest request, CancellationToken ct = default);
    Task<Result<ClassDetailDto>>                   UpdateAsync(int schoolId, int classId, UpdateClassRequest request, CancellationToken ct = default);
    Task<Result>                                   DeleteAsync(int schoolId, int classId, CancellationToken ct = default);
    Task<Result>                                   AssignSubjectAsync(int schoolId, int classId, AssignSubjectRequest request, CancellationToken ct = default);
    Task<Result>                                   CreateScheduleAsync(int schoolId, int classId, CreateScheduleRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TeacherSummaryDto>>> GetAvailableTeachersAsync(int schoolId, CancellationToken ct = default);
    Task<bool>                                     HasScheduleConflictAsync(int schoolId, int semesterId, int teacherId, byte dayOfWeek, byte periodStart, byte periodEnd, int? excludeScheduleId = null, CancellationToken ct = default);

    // ── Student enrollment ────────────────────────────────────────────────────
    Task<Result>                                        EnrollStudentAsync(int schoolId, int classId, int studentUserId, CancellationToken ct = default);
    Task<Result>                                        RemoveStudentAsync(int schoolId, int classId, int studentUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClassStudentDto>>>        GetUnassignedStudentsAsync(int schoolId, int classId, CancellationToken ct = default);

    // ── Select lists for Create form ──────────────────────────────────────────
    Task<Result<IReadOnlyList<GradeLevelSelectDto>>>    GetGradeLevelsAsync(int schoolId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AcademicYearSelectDto>>>  GetAcademicYearsAsync(int schoolId, CancellationToken ct = default);

    // ── Academic configuration (Admin) ────────────────────────────────────────
    Task<Result<IReadOnlyList<AcademicYearConfigDto>>>  GetAcademicYearsForConfigAsync(int schoolId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GradeLevelConfigDto>>>    GetGradeLevelsForConfigAsync(int schoolId, CancellationToken ct = default);
    Task<Result<AcademicYearConfigDto>>                 CreateAcademicYearAsync(int schoolId, CreateAcademicYearRequest request, CancellationToken ct = default);
    Task<Result<GradeLevelConfigDto>>                   CreateGradeLevelAsync(int schoolId, CreateGradeLevelRequest request, CancellationToken ct = default);
}
