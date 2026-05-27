using AutoMapper;
using LuminaTutors.Application.DTOs.HR;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.HR;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class HRService : IHRService
{
    private readonly IUnitOfWork           _uow;
    private readonly IMapper               _mapper;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<HRService>    _logger;

    public HRService(
        IUnitOfWork uow,
        IMapper mapper,
        IPasswordHasher<User> hasher,
        ILogger<HRService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _hasher = hasher;
        _logger = logger;
    }

    // ─── Teachers ─────────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<TeacherDetailDto>>> GetTeachersAsync(
        int schoolId, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        var kw = keyword?.Trim().ToLower();

        var paged = await _uow.TeacherProfiles.GetPagedAsync(
            pageNumber: page,
            pageSize:   pageSize,
            filter: tp =>
                tp.SchoolId == schoolId &&
                (string.IsNullOrEmpty(kw) ||
                 tp.User.FullName.ToLower().Contains(kw) ||
                 tp.User.Email.ToLower().Contains(kw) ||
                 tp.TeacherCode.ToLower().Contains(kw)),
            orderBy: q => q.OrderBy(tp => tp.User.FullName),
            include: q => q.Include(tp => tp.User),
            ct: ct);

        var dtos   = _mapper.Map<List<TeacherDetailDto>>(paged.Items);
        var result = PagedResult<TeacherDetailDto>.Create(dtos, paged.TotalCount, page, pageSize);
        return Result<PagedResult<TeacherDetailDto>>.Success(result);
    }

    public async Task<Result<TeacherDetailDto>> GetTeacherByIdAsync(
        int schoolId, int teacherId, CancellationToken ct = default)
    {
        var profiles = await _uow.TeacherProfiles.FindAsync(
            tp => tp.Id == teacherId && tp.SchoolId == schoolId,
            include: q => q.Include(tp => tp.User),
            ct: ct);

        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return Result<TeacherDetailDto>.Failure("NOT_FOUND", "Giáo viên không tồn tại.");

        return Result<TeacherDetailDto>.Success(_mapper.Map<TeacherDetailDto>(profile));
    }

    public async Task<Result<TeacherDetailDto>> CreateTeacherAsync(
        int schoolId, CreateTeacherRequest request, CancellationToken ct = default)
    {
        var existing = await _uow.Users.FindAsync(
            u => u.SchoolId == schoolId &&
                 u.Email == request.Email.Trim().ToLowerInvariant(),
            ct: ct);

        if (existing.Any())
            return Result<TeacherDetailDto>.Failure("EMAIL_EXISTS", "Email đã được sử dụng.");

        var roles = await _uow.Roles.FindAsync(r => r.RoleCode == nameof(RoleCode.Teacher), ct: ct);
        var teacherRole = roles.FirstOrDefault();
        if (teacherRole is null)
            return Result<TeacherDetailDto>.Failure("CONFIG_ERROR", "Không tìm thấy vai trò giáo viên.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = new User
            {
                SchoolId        = schoolId,
                RoleId          = teacherRole.Id,
                Email           = request.Email.Trim().ToLowerInvariant(),
                FullName        = request.FullName.Trim(),
                PhoneNumber     = request.PhoneNumber?.Trim(),
                IsActive        = true,
                IsEmailVerified = false,
                PasswordHash    = string.Empty
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Email[..Math.Min(8, request.Email.Length)] + "@Lumina1");

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            var profile = new TeacherProfile
            {
                SchoolId              = schoolId,
                UserId                = user.Id,
                TeacherCode           = request.TeacherCode.Trim(),
                SpecializationSubject = request.SpecializationSubject?.Trim(),
                HireDate              = request.HireDate,
                DateOfBirth           = request.DateOfBirth,
                Gender                = request.Gender,
                Qualification         = request.Qualification?.Trim(),
                ContractType          = request.ContractType,
                BankAccountNumber     = request.BankAccountNumber?.Trim(),
                BankName              = request.BankName?.Trim(),
                TaxCode               = request.TaxCode?.Trim()
            };

            await _uow.TeacherProfiles.AddAsync(profile, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Created teacher {TeacherCode} (UserId={UserId}) in school {SchoolId}",
                profile.TeacherCode, user.Id, schoolId);

            return await GetTeacherByIdAsync(schoolId, profile.Id, ct);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CreateTeacher failed for school {SchoolId}", schoolId);
            return Result<TeacherDetailDto>.Failure("INTERNAL_ERROR", "Có lỗi khi tạo giáo viên.");
        }
    }

    public async Task<Result<TeacherDetailDto>> UpdateTeacherAsync(
        int schoolId, int teacherId, CancellationToken ct = default)
    {
        return await GetTeacherByIdAsync(schoolId, teacherId, ct);
    }

    public async Task<Result> DeactivateTeacherAsync(
        int schoolId, int teacherId, CancellationToken ct = default)
    {
        var profiles = await _uow.TeacherProfiles.FindAsync(
            tp => tp.Id == teacherId && tp.SchoolId == schoolId,
            include: q => q.Include(tp => tp.User),
            ct: ct);

        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return Result.Failure("NOT_FOUND", "Giáo viên không tồn tại.");

        profile.User.IsActive = false;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Deactivated teacher {TeacherId} in school {SchoolId}", teacherId, schoolId);
        return Result.Success();
    }

    // ─── Contracts ────────────────────────────────────────────────────────────

    public async Task<Result> CreateContractAsync(
        int schoolId, int createdByUserId, CreateContractRequest request, CancellationToken ct = default)
    {
        var profile = await _uow.TeacherProfiles.GetByIdAsync(request.TeacherId, ct);
        if (profile is null || profile.SchoolId != schoolId)
            return Result.Failure("NOT_FOUND", "Giáo viên không tồn tại.");

        var contract = new TeacherContract
        {
            SchoolId        = schoolId,
            TeacherId       = profile.UserId,
            ContractCode    = request.ContractCode.Trim(),
            ContractType    = request.ContractType,
            StartDate       = request.StartDate,
            EndDate         = request.EndDate,
            BaseSalary      = request.BaseSalary,
            SignedAt        = request.SignedAt,
            DocumentUrl     = request.DocumentUrl?.Trim(),
            Status          = ContractStatus.Active,
            CreatedByUserId = createdByUserId
        };

        await _uow.TeacherContracts.AddAsync(contract, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contract created for teacher {TeacherId}: {Type}, salary {Salary}",
            request.TeacherId, request.ContractType, request.BaseSalary);

        return Result.Success();
    }

    // ─── Payroll ──────────────────────────────────────────────────────────────

    public async Task<Result<PayrollDto>> CreatePayrollAsync(
        int schoolId, int createdByUserId, CreatePayrollRequest request, CancellationToken ct = default)
    {
        var profile = await _uow.TeacherProfiles.GetByIdAsync(request.TeacherId, ct);
        if (profile is null || profile.SchoolId != schoolId)
            return Result<PayrollDto>.Failure("NOT_FOUND", "Giáo viên không tồn tại.");

        var duplicate = await _uow.Payrolls.FindAsync(
            p => p.UserId        == profile.UserId &&
                 p.PayrollMonth  == request.Month &&
                 p.PayrollYear   == request.Year,
            ct: ct);

        if (duplicate.Any())
            return Result<PayrollDto>.Failure("DUPLICATE", "Bảng lương tháng này đã tồn tại.");

        var payroll = new Payroll
        {
            SchoolId              = schoolId,
            UserId                = profile.UserId,
            PayrollMonth          = request.Month,
            PayrollYear           = request.Year,
            BaseSalary            = request.BaseSalary,
            TeachingAllowance     = request.TeachingAllowance,
            PositionAllowance     = request.PositionAllowance,
            OvertimePay           = request.OvertimePay,
            Bonus                 = request.Bonus,
            InsuranceDeduction    = request.InsuranceDeduction,
            TaxDeduction          = request.TaxDeduction,
            OtherDeductions       = request.OtherDeductions,
            Note   = request.Note?.Trim(),
            Status = PayrollStatus.Draft
        };

        await _uow.Payrolls.AddAsync(payroll, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<PayrollDto>.Success(_mapper.Map<PayrollDto>(payroll));
    }

    public async Task<Result<IReadOnlyList<PayrollDto>>> GetPayrollsAsync(
        int schoolId, byte month, short year, CancellationToken ct = default)
    {
        var payrolls = await _uow.Payrolls.FindAsync(
            p => p.SchoolId     == schoolId &&
                 p.PayrollMonth == month &&
                 p.PayrollYear  == year,
            include: q => q.Include(p => p.User),
            ct: ct);

        var dtos = _mapper.Map<List<PayrollDto>>(payrolls);
        return Result<IReadOnlyList<PayrollDto>>.Success(dtos);
    }

    public async Task<Result> ApprovePayrollAsync(
        int payrollId, int approvedByUserId, CancellationToken ct = default)
    {
        var payroll = await _uow.Payrolls.GetByIdAsync(payrollId, ct);
        if (payroll is null)
            return Result.Failure("NOT_FOUND", "Bảng lương không tồn tại.");

        if (payroll.Status == PayrollStatus.Approved)
            return Result.Failure("ALREADY_APPROVED", "Bảng lương đã được duyệt.");

        payroll.Status           = PayrollStatus.Approved;
        payroll.ApprovedByUserId = approvedByUserId;
        payroll.ApprovedAt       = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Payroll {Id} approved by user {UserId}", payrollId, approvedByUserId);
        return Result.Success();
    }
}
