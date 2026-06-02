using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly IUnitOfWork                   _uow;
    private readonly ILogger<LeaveRequestService>  _logger;

    public LeaveRequestService(IUnitOfWork uow, ILogger<LeaveRequestService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    public async Task<Result<LeaveRequestDto>> CreateAsync(
        int schoolId, int parentId, CreateLeaveRequestRequest request, CancellationToken ct = default)
    {
        if (request.ToDate < request.FromDate)
            return Result<LeaveRequestDto>.Failure("Ngày kết thúc phải sau ngày bắt đầu.", "INVALID_DATES");

        // Verify student belongs to the same school
        var student = (await _uow.Users.FindAsync(
            u => u.Id == request.StudentId && u.SchoolId == schoolId, ct: ct)).FirstOrDefault();

        if (student is null)
            return Result<LeaveRequestDto>.Failure("Không tìm thấy học sinh trong trường.", "STUDENT_NOT_FOUND");

        var entity = new LeaveRequest
        {
            SchoolId  = schoolId,
            ParentId  = parentId,
            StudentId = request.StudentId,
            FromDate  = request.FromDate,
            ToDate    = request.ToDate,
            Reason    = request.Reason.Trim(),
            Status    = LeaveRequestStatus.Pending
        };

        await _uow.LeaveRequests.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("LeaveRequest {Id} created by parent {ParentId} for student {StudentId}",
            entity.Id, parentId, request.StudentId);

        var dto = await BuildDtoAsync(entity, ct);
        return Result<LeaveRequestDto>.Success(dto);
    }

    // ─── GetByParent ──────────────────────────────────────────────────────────

    public async Task<Result<LeaveRequestListDto>> GetByParentAsync(int parentId, CancellationToken ct = default)
    {
        var requests = await _uow.LeaveRequests.FindAsync(
            r => r.ParentId == parentId,
            include: q => q.Include(r => r.Student).Include(r => r.Parent).Include(r => r.ReviewedBy),
            ct: ct);

        var sorted  = requests.OrderByDescending(r => r.CreatedAt).ToList();
        var dtos    = await BuildDtosAsync(sorted, ct);
        var pending = dtos.Count(d => d.Status == "Pending");

        return Result<LeaveRequestListDto>.Success(new LeaveRequestListDto(dtos, dtos.Count, pending));
    }

    // ─── GetBySchool ──────────────────────────────────────────────────────────

    public async Task<Result<LeaveRequestListDto>> GetBySchoolAsync(
        int schoolId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var all = await _uow.LeaveRequests.FindAsync(
            r => r.SchoolId == schoolId &&
                 (status == null || r.Status.ToString() == status),
            include: q => q.Include(r => r.Student).Include(r => r.Parent).Include(r => r.ReviewedBy),
            ct: ct);

        var sorted  = all.OrderByDescending(r => r.CreatedAt).ToList();
        var total   = sorted.Count;
        var pending = sorted.Count(r => r.Status == LeaveRequestStatus.Pending);
        var paged   = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos    = await BuildDtosAsync(paged, ct);

        return Result<LeaveRequestListDto>.Success(new LeaveRequestListDto(dtos, total, pending));
    }

    // ─── Review ───────────────────────────────────────────────────────────────

    public async Task<Result> ReviewAsync(
        int requestId, int reviewerUserId, ReviewLeaveRequestRequest request, CancellationToken ct = default)
    {
        var entity = await _uow.LeaveRequests.FindOneAsync(r => r.Id == requestId, ct);
        if (entity is null)
            return Result.Failure("Không tìm thấy đơn xin nghỉ.", "NOT_FOUND");

        if (entity.Status != LeaveRequestStatus.Pending)
            return Result.Failure("Đơn này đã được xử lý trước đó.", "ALREADY_REVIEWED");

        entity.Status           = request.Approved ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        entity.ReviewedByUserId = reviewerUserId;
        entity.ReviewedAt       = DateTime.UtcNow;
        entity.ReviewNote       = request.Note?.Trim();

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("LeaveRequest {Id} {Status} by user {UserId}",
            requestId, entity.Status, reviewerUserId);

        return Result.Success();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<List<LeaveRequestDto>> BuildDtosAsync(List<LeaveRequest> requests, CancellationToken ct)
    {
        var result = new List<LeaveRequestDto>(requests.Count);
        foreach (var r in requests)
            result.Add(await BuildDtoAsync(r, ct));
        return result;
    }

    private async Task<LeaveRequestDto> BuildDtoAsync(LeaveRequest r, CancellationToken ct)
    {
        // Load student profile for code
        var profile = (await _uow.StudentProfiles.FindAsync(
            p => p.UserId == r.StudentId, ct: ct)).FirstOrDefault();

        // Load active class enrollment
        var enrollment = (await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == r.StudentId && e.Status == EnrollmentStatus.Active,
            include: q => q.Include(e => e.Class),
            ct: ct)).FirstOrDefault();

        // Ensure nav props are loaded (may already be from Include)
        var studentName = r.Student?.FullName ?? "—";
        var parentName  = r.Parent?.FullName  ?? "—";

        return new LeaveRequestDto(
            Id:          r.Id,
            StudentName: studentName,
            StudentCode: profile?.StudentCode ?? r.StudentId.ToString(),
            ClassName:   enrollment?.Class?.ClassName ?? "—",
            ParentName:  parentName,
            FromDate:    r.FromDate,
            ToDate:      r.ToDate,
            DayCount:    r.ToDate.DayNumber - r.FromDate.DayNumber + 1,
            Reason:      r.Reason,
            Status:      r.Status.ToString(),
            ReviewNote:  r.ReviewNote,
            ReviewedBy:  r.ReviewedBy?.FullName,
            ReviewedAt:  r.ReviewedAt,
            CreatedAt:   r.CreatedAt
        );
    }
}
