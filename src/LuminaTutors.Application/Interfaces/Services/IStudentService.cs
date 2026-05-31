using LuminaTutors.Application.DTOs.Student;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IStudentService
{
    Task<Result<PagedResult<StudentSummaryDto>>> SearchAsync(int schoolId, StudentSearchRequest request, CancellationToken ct = default);
    Task<Result<StudentDetailDto>>               GetByIdAsync(int schoolId, int studentId, CancellationToken ct = default);
    Task<Result<StudentDetailDto>>               CreateAsync(int schoolId, CreateStudentRequest request, CancellationToken ct = default);
    Task<Result<StudentDetailDto>>               UpdateAsync(int schoolId, int studentId, UpdateStudentRequest request, CancellationToken ct = default);
    Task<Result>                                 DeactivateAsync(int schoolId, int studentId, CancellationToken ct = default);
    Task<Result>                                 EnrollAsync(int schoolId, int studentId, EnrollStudentRequest request, CancellationToken ct = default);
    Task<Result>                                 TransferAsync(int schoolId, int studentId, TransferStudentRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StudentSummaryDto>>> GetByClassAsync(int classId, CancellationToken ct = default);
}
