using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface ILeaveRequestService
{
    // Parent submits a leave request for their child
    Task<Result<LeaveRequestDto>> CreateAsync(int schoolId, int parentId, CreateLeaveRequestRequest request, CancellationToken ct = default);

    // Parent views their own submitted requests
    Task<Result<LeaveRequestListDto>> GetByParentAsync(int parentId, CancellationToken ct = default);

    // Supervisor views all pending/recent requests for the school
    Task<Result<LeaveRequestListDto>> GetBySchoolAsync(int schoolId, string? status, int page, int pageSize, CancellationToken ct = default);

    // Supervisor approves or rejects
    Task<Result> ReviewAsync(int requestId, int reviewerUserId, ReviewLeaveRequestRequest request, CancellationToken ct = default);
}
