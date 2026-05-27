using AutoMapper;
using LuminaTutors.Application.DTOs.Student;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class StudentService : IStudentService
{
    private readonly IUnitOfWork           _uow;
    private readonly IMapper               _mapper;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IUnitOfWork uow,
        IMapper mapper,
        IPasswordHasher<User> hasher,
        ILogger<StudentService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _hasher = hasher;
        _logger = logger;
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<StudentSummaryDto>>> SearchAsync(
        int schoolId, StudentSearchRequest request, CancellationToken ct = default)
    {
        var keyword = request.Keyword?.Trim().ToLower();

        // Pre-load enrolled student user IDs if class filter is requested
        List<int>? activeStudentUserIds = null;
        if (request.ClassId.HasValue)
        {
            var classEnrollments = await _uow.ClassEnrollments.FindAsync(
                e => e.ClassId == request.ClassId.Value && e.Status == EnrollmentStatus.Active,
                ct: ct);
            activeStudentUserIds = classEnrollments.Select(e => e.StudentId).ToList();
        }

        var paged = await _uow.StudentProfiles.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize:   request.PageSize,
            filter: sp =>
                sp.SchoolId == schoolId &&
                (string.IsNullOrEmpty(keyword) ||
                 sp.User.FullName.ToLower().Contains(keyword) ||
                 sp.User.Email.ToLower().Contains(keyword) ||
                 sp.StudentCode.ToLower().Contains(keyword)) &&
                (activeStudentUserIds == null || activeStudentUserIds.Contains(sp.UserId)) &&
                (!request.IsActive.HasValue || sp.User.IsActive == request.IsActive.Value),
            orderBy: q => q.OrderBy(sp => sp.User.FullName),
            include: q => q.Include(sp => sp.User),
            ct: ct);

        var dtos   = _mapper.Map<List<StudentSummaryDto>>(paged.Items);
        var result = PagedResult<StudentSummaryDto>.Create(dtos, paged.TotalCount, request.PageNumber, request.PageSize);
        return Result<PagedResult<StudentSummaryDto>>.Success(result);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    public async Task<Result<StudentDetailDto>> GetByIdAsync(int schoolId, int studentId, CancellationToken ct = default)
    {
        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => sp.Id == studentId && sp.SchoolId == schoolId,
            include: q => q
                .Include(sp => sp.User)
                    .ThenInclude(u => u.StudentRelations)
                        .ThenInclude(pr => pr.Parent),
            ct: ct);

        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return Result<StudentDetailDto>.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        var parentDtos = profile.User.StudentRelations.Select(pr => new ParentInfoDto(
            ParentUserId:     pr.ParentUserId,
            FullName:         pr.Parent.FullName,
            PhoneNumber:      pr.Parent.PhoneNumber,
            Relationship:     pr.Relationship,
            IsPrimaryContact: pr.IsPrimaryContact)).ToList();

        var dto = _mapper.Map<StudentDetailDto>(profile) with { Parents = parentDtos };
        return Result<StudentDetailDto>.Success(dto);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    public async Task<Result<StudentDetailDto>> CreateAsync(
        int schoolId, CreateStudentRequest request, CancellationToken ct = default)
    {
        var existing = await _uow.Users.FindAsync(
            u => u.SchoolId == schoolId && u.Email == request.Email.Trim().ToLowerInvariant(),
            ct: ct);

        if (existing.Any())
            return Result<StudentDetailDto>.Failure("EMAIL_EXISTS", "Email đã được sử dụng trong trường này.");

        var roles = await _uow.Roles.FindAsync(r => r.RoleCode == nameof(RoleCode.Student), ct: ct);
        var studentRole = roles.FirstOrDefault();
        if (studentRole is null)
            return Result<StudentDetailDto>.Failure("CONFIG_ERROR", "Không tìm thấy vai trò học sinh.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = new User
            {
                SchoolId        = schoolId,
                RoleId          = studentRole.Id,
                Email           = request.Email.Trim().ToLowerInvariant(),
                FullName        = request.FullName.Trim(),
                IsActive        = true,
                IsEmailVerified = false,
                PasswordHash    = string.Empty
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Email[..Math.Min(8, request.Email.Length)] + "@Lumina1");
            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            var profile = new StudentProfile
            {
                SchoolId          = schoolId,
                UserId            = user.Id,
                StudentCode       = request.StudentCode.Trim(),
                DateOfBirth       = request.DateOfBirth,
                Gender            = request.Gender,
                PlaceOfBirth      = request.PlaceOfBirth?.Trim(),
                PermanentAddress  = request.PermanentAddress?.Trim(),
                EthnicGroup       = request.EthnicGroup?.Trim(),
                AdmissionDate     = request.AdmissionDate,
                AdmissionType     = request.AdmissionType
            };

            await _uow.StudentProfiles.AddAsync(profile, ct);
            await _uow.SaveChangesAsync(ct);

            // Enroll in initial class if provided
            if (request.InitialClassId.HasValue)
            {
                var enrollment = new ClassEnrollment
                {
                    ClassId      = request.InitialClassId.Value,
                    StudentId    = user.Id,
                    Status       = EnrollmentStatus.Active,
                    EnrolledDate = request.AdmissionDate ?? DateOnly.FromDateTime(DateTime.Today)
                };
                await _uow.ClassEnrollments.AddAsync(enrollment, ct);
                await _uow.SaveChangesAsync(ct);
            }

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Created student {StudentCode} (UserId={UserId}) in school {SchoolId}",
                profile.StudentCode, user.Id, schoolId);

            return await GetByIdAsync(schoolId, profile.Id, ct);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CreateStudent failed for school {SchoolId}", schoolId);
            return Result<StudentDetailDto>.Failure("INTERNAL_ERROR", "Có lỗi khi tạo học sinh. Vui lòng thử lại.");
        }
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    public async Task<Result<StudentDetailDto>> UpdateAsync(
        int schoolId, int studentId, UpdateStudentRequest request, CancellationToken ct = default)
    {
        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => sp.Id == studentId && sp.SchoolId == schoolId,
            include: q => q.Include(sp => sp.User),
            ct: ct);

        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return Result<StudentDetailDto>.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        profile.User.FullName    = request.FullName.Trim();
        profile.User.PhoneNumber = request.PhoneNumber?.Trim();
        profile.DateOfBirth      = request.DateOfBirth;
        profile.Gender           = request.Gender;
        profile.PlaceOfBirth     = request.PlaceOfBirth?.Trim();
        profile.PermanentAddress = request.PermanentAddress?.Trim();
        profile.EthnicGroup      = request.EthnicGroup?.Trim();

        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, studentId, ct);
    }

    // ─── Deactivate ───────────────────────────────────────────────────────────

    public async Task<Result> DeactivateAsync(int schoolId, int studentId, CancellationToken ct = default)
    {
        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => sp.Id == studentId && sp.SchoolId == schoolId,
            include: q => q.Include(sp => sp.User),
            ct: ct);

        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return Result.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        profile.User.IsActive = false;

        var enrollments = await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == profile.UserId && e.Status == EnrollmentStatus.Active,
            ct: ct);

        foreach (var en in enrollments)
            en.Status = EnrollmentStatus.Withdrawn;

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Deactivated student {StudentId} in school {SchoolId}", studentId, schoolId);
        return Result.Success();
    }

    // ─── Enroll ───────────────────────────────────────────────────────────────

    public async Task<Result> EnrollAsync(
        int schoolId, int studentId, EnrollStudentRequest request, CancellationToken ct = default)
    {
        var profileList = await _uow.StudentProfiles.FindAsync(
            sp => sp.Id == studentId && sp.SchoolId == schoolId,
            ct: ct);
        var profile = profileList.FirstOrDefault();
        if (profile is null)
            return Result.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        var classEntity = await _uow.Classes.GetByIdAsync(request.ClassId, ct);
        if (classEntity is null || classEntity.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Lớp học không tồn tại.");

        var duplicate = await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == profile.UserId &&
                 e.Class.AcademicYearId == classEntity.AcademicYearId &&
                 e.Status == EnrollmentStatus.Active,
            include: q => q.Include(e => e.Class),
            ct: ct);

        if (duplicate.Any())
            return Result.Failure("ALREADY_ENROLLED", "Học sinh đã được xếp lớp trong năm học này.");

        var enrollment = new ClassEnrollment
        {
            StudentId    = profile.UserId,
            ClassId      = request.ClassId,
            Status       = EnrollmentStatus.Active,
            EnrolledDate = request.EnrolledDate ?? DateOnly.FromDateTime(DateTime.Today)
        };

        await _uow.ClassEnrollments.AddAsync(enrollment, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── Transfer ─────────────────────────────────────────────────────────────

    public async Task<Result> TransferAsync(
        int schoolId, int studentId, TransferStudentRequest request, CancellationToken ct = default)
    {
        var profileList = await _uow.StudentProfiles.FindAsync(
            sp => sp.Id == studentId && sp.SchoolId == schoolId,
            ct: ct);
        var profile = profileList.FirstOrDefault();
        if (profile is null)
            return Result.Failure("NOT_FOUND", "Học sinh không tồn tại.");

        var activeEnrollments = await _uow.ClassEnrollments.FindAsync(
            e => e.StudentId == profile.UserId && e.Status == EnrollmentStatus.Active,
            ct: ct);

        var current = activeEnrollments.FirstOrDefault();
        if (current is null)
            return Result.Failure("NOT_FOUND", "Học sinh không có đăng ký lớp hiện tại.");

        var toClass = await _uow.Classes.GetByIdAsync(request.NewClassId, ct);
        if (toClass is null || toClass.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Lớp đích không tồn tại.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            current.Status = EnrollmentStatus.Transferred;

            var newEnrollment = new ClassEnrollment
            {
                StudentId    = profile.UserId,
                ClassId      = request.NewClassId,
                Status       = EnrollmentStatus.Active,
                EnrolledDate = DateOnly.FromDateTime(DateTime.Today),
                TransferNote = request.TransferNote?.Trim()
            };

            await _uow.ClassEnrollments.AddAsync(newEnrollment, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Transferred student {StudentId}: class {From} → {To}",
                studentId, current.ClassId, request.NewClassId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "TransferStudent failed");
            return Result.Failure("INTERNAL_ERROR", "Có lỗi khi chuyển lớp.");
        }
    }

    // ─── GetByClass ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<StudentSummaryDto>>> GetByClassAsync(
        int classId, CancellationToken ct = default)
    {
        var enrollments = await _uow.ClassEnrollments.FindAsync(
            e => e.ClassId == classId && e.Status == EnrollmentStatus.Active,
            ct: ct);

        var studentUserIds = enrollments.Select(e => e.StudentId).ToList();

        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => studentUserIds.Contains(sp.UserId),
            include: q => q.Include(sp => sp.User),
            ct: ct);

        var dtos = _mapper.Map<List<StudentSummaryDto>>(profiles);
        return Result<IReadOnlyList<StudentSummaryDto>>.Success(dtos);
    }
}
