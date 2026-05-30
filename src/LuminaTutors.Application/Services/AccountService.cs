using LuminaTutors.Application.DTOs.Account;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class AccountService : IAccountService
{
    private readonly IUnitOfWork           _uow;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<AccountService> _logger;

    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "STUDENT", "TEACHER", "SUPERVISOR", "PARENT" };

    public AccountService(IUnitOfWork uow, IPasswordHasher<User> hasher, ILogger<AccountService> logger)
    {
        _uow    = uow;
        _hasher = hasher;
        _logger = logger;
    }

    // ─── List ─────────────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<AccountListItemDto>>> GetAccountsAsync(
        int schoolId, AccountFilterRequest filter, CancellationToken ct = default)
    {
        var kw = filter.Keyword?.Trim().ToLower();

        var paged = await _uow.Users.GetPagedAsync(
            pageNumber: filter.Page,
            pageSize:   filter.PageSize,
            filter: u =>
                u.SchoolId == schoolId &&
                (string.IsNullOrEmpty(filter.RoleCode) || u.Role.RoleCode == filter.RoleCode.ToUpper()) &&
                (filter.IsActive == null || u.IsActive == filter.IsActive) &&
                (string.IsNullOrEmpty(kw) ||
                 u.FullName.ToLower().Contains(kw) ||
                 u.Email.ToLower().Contains(kw) ||
                 (u.PhoneNumber != null && u.PhoneNumber.Contains(kw))) &&
                AllowedRoles.Contains(u.Role.RoleCode),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            include: q => q
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile).ThenInclude(tp => tp!.PrimarySubject)
                .Include(u => u.SupervisorProfile)
                .Include(u => u.ParentProfile),
            ct: ct);

        var items = paged.Items.Select(u => ToListItem(u)).ToList();
        var result = PagedResult<AccountListItemDto>.Create(items, paged.TotalCount, filter.Page, filter.PageSize);
        return Result<PagedResult<AccountListItemDto>>.Success(result);
    }

    // ─── Detail ───────────────────────────────────────────────────────────────

    public async Task<Result<AccountDetailDto>> GetAccountByIdAsync(
        int schoolId, int userId, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId && u.SchoolId == schoolId,
            include: q => q
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile).ThenInclude(tp => tp!.PrimarySubject)
                .Include(u => u.SupervisorProfile)
                .Include(u => u.ParentProfile),
            ct: ct);

        var user = users.FirstOrDefault();
        if (user is null)
            return Result<AccountDetailDto>.Failure("NOT_FOUND", "Tài khoản không tồn tại.");

        // Check role is one of the 4 allowed
        if (!AllowedRoles.Contains(user.Role.RoleCode))
            return Result<AccountDetailDto>.Failure("FORBIDDEN", "Không thể quản lý tài khoản này.");

        return Result<AccountDetailDto>.Success(await ToDetailAsync(user, schoolId, ct));
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    public async Task<Result<AccountDetailDto>> CreateAccountAsync(
        int schoolId, CreateAccountRequest req, CancellationToken ct = default)
    {
        var roleCode = req.RoleCode.ToUpper();
        if (!AllowedRoles.Contains(roleCode))
            return Result<AccountDetailDto>.Failure("INVALID_ROLE", "Vai trò không hợp lệ.");

        // Check email uniqueness in school
        var existing = await _uow.Users.FindAsync(
            u => u.SchoolId == schoolId && u.Email == req.Email.Trim().ToLowerInvariant(), ct: ct);
        if (existing.Any())
            return Result<AccountDetailDto>.Failure("EMAIL_EXISTS", "Email đã được sử dụng trong trường này.");

        // Load role entity
        var roles = await _uow.Roles.FindAsync(r => r.RoleCode == roleCode, ct: ct);
        var role  = roles.FirstOrDefault();
        if (role is null)
            return Result<AccountDetailDto>.Failure("CONFIG_ERROR", $"Không tìm thấy vai trò {roleCode}.");

        User? createdUser = null;
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                // 1. Create User
                var user = new User
                {
                    SchoolId        = schoolId,
                    RoleId          = role.Id,
                    Email           = req.Email.Trim().ToLowerInvariant(),
                    FullName        = req.FullName.Trim(),
                    PhoneNumber     = req.PhoneNumber?.Trim(),
                    AvatarUrl       = req.AvatarUrl,
                    IsActive        = true,
                    IsEmailVerified = false,
                    PasswordHash    = string.Empty
                };
                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                await _uow.Users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);   // flushes so user.Id is populated

                // 2. Create role-specific profile
                switch (roleCode)
                {
                    case "STUDENT":
                        var code = $"HS{DateTime.UtcNow:yyMM}{user.Id:D4}";
                        await _uow.StudentProfiles.AddAsync(new StudentProfile
                        {
                            UserId      = user.Id,
                            SchoolId    = schoolId,
                            StudentCode = code,
                            DateOfBirth = req.DateOfBirth,
                            Gender      = req.Gender
                        }, ct);
                        await _uow.SaveChangesAsync(ct);

                        // Enroll in class if specified
                        if (req.ClassId.HasValue)
                        {
                            var cls = await _uow.Classes.GetByIdAsync(req.ClassId.Value, ct);
                            if (cls != null && cls.SchoolId == schoolId)
                            {
                                await _uow.ClassEnrollments.AddAsync(new Domain.Entities.Academic.ClassEnrollment
                                {
                                    ClassId      = req.ClassId.Value,
                                    StudentId    = user.Id,
                                    EnrolledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                                    Status       = EnrollmentStatus.Active
                                }, ct);
                                await _uow.SaveChangesAsync(ct);
                            }
                        }

                        // Auto-create linked parent account if requested
                        if (req.CreateLinkedParent
                            && !string.IsNullOrWhiteSpace(req.ParentFullName)
                            && !string.IsNullOrWhiteSpace(req.ParentEmail))
                        {
                            var parentEmail = req.ParentEmail.Trim().ToLowerInvariant();
                            var parentEmailExists = await _uow.Users.AnyAsync(
                                u => u.SchoolId == schoolId && u.Email == parentEmail, ct);

                            if (!parentEmailExists)
                            {
                                var parentRoles = await _uow.Roles.FindAsync(r => r.RoleCode == "PARENT", ct: ct);
                                var parentRole  = parentRoles.FirstOrDefault();
                                if (parentRole is not null)
                                {
                                    var parentUser = new User
                                    {
                                        SchoolId        = schoolId,
                                        RoleId          = parentRole.Id,
                                        Email           = parentEmail,
                                        FullName        = req.ParentFullName.Trim(),
                                        PhoneNumber     = req.ParentPhoneNumber?.Trim(),
                                        IsActive        = true,
                                        IsEmailVerified = false,
                                        PasswordHash    = string.Empty
                                    };
                                    var parentPass = string.IsNullOrWhiteSpace(req.ParentPassword)
                                        ? "Parent@123" : req.ParentPassword;
                                    parentUser.PasswordHash = _hasher.HashPassword(parentUser, parentPass);

                                    await _uow.Users.AddAsync(parentUser, ct);
                                    await _uow.SaveChangesAsync(ct);

                                    await _uow.ParentProfiles.AddAsync(new ParentProfile
                                    {
                                        UserId   = parentUser.Id,
                                        SchoolId = schoolId
                                    }, ct);
                                    await _uow.SaveChangesAsync(ct);

                                    await _uow.ParentStudentRelations.AddAsync(new ParentStudentRelation
                                    {
                                        ParentUserId     = parentUser.Id,
                                        StudentUserId    = user.Id,
                                        Relationship     = req.ParentRelationship ?? "Phụ huynh",
                                        IsPrimaryContact = true
                                    }, ct);
                                    await _uow.SaveChangesAsync(ct);

                                    _logger.LogInformation(
                                        "Auto-created PARENT account {Email} linked to STUDENT {StudentId}",
                                        parentEmail, user.Id);
                                }
                            }
                            else
                            {
                                // Parent email already exists — just create the relation
                                var existingParents = await _uow.Users.FindAsync(
                                    u => u.SchoolId == schoolId && u.Email == parentEmail, ct: ct);
                                var existingParent = existingParents.FirstOrDefault();
                                if (existingParent is not null)
                                {
                                    var alreadyLinked = await _uow.ParentStudentRelations.AnyAsync(
                                        r => r.ParentUserId == existingParent.Id && r.StudentUserId == user.Id, ct);
                                    if (!alreadyLinked)
                                    {
                                        await _uow.ParentStudentRelations.AddAsync(new ParentStudentRelation
                                        {
                                            ParentUserId     = existingParent.Id,
                                            StudentUserId    = user.Id,
                                            Relationship     = req.ParentRelationship ?? "Phụ huynh",
                                            IsPrimaryContact = true
                                        }, ct);
                                        await _uow.SaveChangesAsync(ct);
                                    }
                                }
                            }
                        }
                        break;

                    case "TEACHER":
                        // Always derive SpecializationSubject from the Subject entity when PrimarySubjectId is given.
                        // This guarantees the two fields never desync regardless of JS behaviour.
                        string? specSubjectCreate = req.SpecializationSubject?.Trim();
                        if (req.PrimarySubjectId.HasValue)
                        {
                            var sub = await _uow.Subjects.GetByIdAsync(req.PrimarySubjectId.Value, ct);
                            if (sub != null) specSubjectCreate = sub.SubjectName;
                        }
                        await _uow.TeacherProfiles.AddAsync(new TeacherProfile
                        {
                            UserId                = user.Id,
                            SchoolId              = schoolId,
                            TeacherCode           = $"GV{DateTime.UtcNow:yyMM}{user.Id:D4}",
                            DateOfBirth           = req.DateOfBirth,
                            Gender                = req.Gender,
                            SpecializationSubject = specSubjectCreate,
                            PrimarySubjectId      = req.PrimarySubjectId,
                            Qualification         = req.Qualification?.Trim(),
                            HireDate              = DateOnly.FromDateTime(DateTime.UtcNow)
                        }, ct);
                        await _uow.SaveChangesAsync(ct);
                        break;

                    case "SUPERVISOR":
                        await _uow.SupervisorProfiles.AddAsync(new SupervisorProfile
                        {
                            UserId         = user.Id,
                            SchoolId       = schoolId,
                            SupervisorCode = $"GT{DateTime.UtcNow:yyMM}{user.Id:D4}",
                            DateOfBirth    = req.DateOfBirth,
                            Gender         = req.Gender,
                            HireDate       = DateOnly.FromDateTime(DateTime.UtcNow)
                        }, ct);
                        await _uow.SaveChangesAsync(ct);
                        break;

                    case "PARENT":
                        await _uow.ParentProfiles.AddAsync(new ParentProfile
                        {
                            UserId      = user.Id,
                            SchoolId    = schoolId,
                            Occupation  = req.Occupation?.Trim(),
                            WorkAddress = req.WorkAddress?.Trim()
                        }, ct);
                        await _uow.SaveChangesAsync(ct);

                        if (req.LinkedStudentUserId.HasValue)
                        {
                            await _uow.ParentStudentRelations.AddAsync(new ParentStudentRelation
                            {
                                ParentUserId     = user.Id,
                                StudentUserId    = req.LinkedStudentUserId.Value,
                                Relationship     = req.Relationship ?? "Phụ huynh",
                                IsPrimaryContact = true
                            }, ct);
                            await _uow.SaveChangesAsync(ct);
                        }
                        break;
                }

                createdUser = user;
            }, ct);

            _logger.LogInformation("Created {Role} account UserId={UserId} in school {SchoolId}",
                roleCode, createdUser!.Id, schoolId);
            return await GetAccountByIdAsync(schoolId, createdUser.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAccount failed for school {SchoolId}", schoolId);
            return Result<AccountDetailDto>.Failure("INTERNAL_ERROR", "Có lỗi khi tạo tài khoản. Vui lòng thử lại.");
        }
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    public async Task<Result<AccountDetailDto>> UpdateAccountAsync(
        int schoolId, int userId, UpdateAccountRequest req, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId && u.SchoolId == schoolId,
            include: q => q
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .Include(u => u.SupervisorProfile)
                .Include(u => u.ParentProfile)
                .Include(u => u.ParentRelations),
            ct: ct);

        var user = users.FirstOrDefault();
        if (user is null)
            return Result<AccountDetailDto>.Failure("NOT_FOUND", "Tài khoản không tồn tại.");

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                // Update base user
                user.FullName    = req.FullName.Trim();
                user.PhoneNumber = req.PhoneNumber?.Trim();
                user.IsActive    = req.IsActive;
                if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;

                var roleCode = user.Role.RoleCode;

                switch (roleCode)
                {
                    case "STUDENT":
                        if (user.StudentProfile != null)
                        {
                            user.StudentProfile.DateOfBirth = req.DateOfBirth;
                            user.StudentProfile.Gender      = req.Gender;
                        }
                        if (req.ClassId.HasValue)
                        {
                            var existing = await _uow.ClassEnrollments.FindAsync(
                                e => e.StudentId == userId && e.Status == EnrollmentStatus.Active, ct: ct);
                            foreach (var e in existing) e.Status = EnrollmentStatus.Withdrawn;
                            await _uow.SaveChangesAsync(ct);

                            var cls = await _uow.Classes.GetByIdAsync(req.ClassId.Value, ct);
                            if (cls != null && cls.SchoolId == schoolId)
                            {
                                await _uow.ClassEnrollments.AddAsync(new Domain.Entities.Academic.ClassEnrollment
                                {
                                    ClassId      = req.ClassId.Value,
                                    StudentId    = userId,
                                    EnrolledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                                    Status       = EnrollmentStatus.Active
                                }, ct);
                            }
                        }
                        break;

                    case "TEACHER":
                        if (user.TeacherProfile != null)
                        {
                            // Derive SpecializationSubject from Subject entity (source of truth = PrimarySubjectId)
                            string? specSubjectUpdate = req.SpecializationSubject?.Trim();
                            if (req.PrimarySubjectId.HasValue)
                            {
                                var sub = await _uow.Subjects.GetByIdAsync(req.PrimarySubjectId.Value, ct);
                                if (sub != null) specSubjectUpdate = sub.SubjectName;
                            }
                            user.TeacherProfile.DateOfBirth           = req.DateOfBirth;
                            user.TeacherProfile.Gender                = req.Gender;
                            user.TeacherProfile.SpecializationSubject = specSubjectUpdate;
                            user.TeacherProfile.PrimarySubjectId      = req.PrimarySubjectId;
                            user.TeacherProfile.Qualification         = req.Qualification?.Trim();
                        }
                        break;

                    case "SUPERVISOR":
                        if (user.SupervisorProfile != null)
                        {
                            user.SupervisorProfile.DateOfBirth = req.DateOfBirth;
                            user.SupervisorProfile.Gender      = req.Gender;
                        }
                        break;

                    case "PARENT":
                        if (user.ParentProfile != null)
                        {
                            user.ParentProfile.Occupation  = req.Occupation?.Trim();
                            user.ParentProfile.WorkAddress = req.WorkAddress?.Trim();
                        }
                        if (req.LinkedStudentUserId.HasValue)
                        {
                            var existing = await _uow.ParentStudentRelations.FindAsync(
                                r => r.ParentUserId == userId, ct: ct);
                            foreach (var r in existing)
                                _uow.ParentStudentRelations.Remove(r);
                            await _uow.SaveChangesAsync(ct);

                            await _uow.ParentStudentRelations.AddAsync(new ParentStudentRelation
                            {
                                ParentUserId     = userId,
                                StudentUserId    = req.LinkedStudentUserId.Value,
                                Relationship     = req.Relationship ?? "Phụ huynh",
                                IsPrimaryContact = true
                            }, ct);
                        }
                        break;
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Updated account UserId={UserId}", userId);
            return await GetAccountByIdAsync(schoolId, userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAccount failed for UserId={UserId}", userId);
            return Result<AccountDetailDto>.Failure("INTERNAL_ERROR", "Có lỗi khi cập nhật tài khoản.");
        }
    }

    // ─── Toggle Active ────────────────────────────────────────────────────────

    public async Task<Result> ToggleActiveAsync(int schoolId, int userId, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId && u.SchoolId == schoolId, ct: ct);
        var user = users.FirstOrDefault();
        if (user is null) return Result.Failure("NOT_FOUND", "Tài khoản không tồn tại.");

        user.IsActive = !user.IsActive;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Account UserId={UserId} IsActive set to {Active}", userId, user.IsActive);
        return Result.Success();
    }

    // ─── Reset Password ───────────────────────────────────────────────────────

    public async Task<Result> ResetPasswordAsync(int schoolId, int userId, string newPassword, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId && u.SchoolId == schoolId, ct: ct);
        var user = users.FirstOrDefault();
        if (user is null) return Result.Failure("NOT_FOUND", "Tài khoản không tồn tại.");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Password reset for UserId={UserId} by admin", userId);
        return Result.Success();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    public async Task<Result> DeleteAccountAsync(int schoolId, int userId, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId && u.SchoolId == schoolId,
            include: q => q.Include(u => u.Role),
            ct: ct);
        var user = users.FirstOrDefault();
        if (user is null) return Result.Failure("NOT_FOUND", "Tài khoản không tồn tại.");
        if (!AllowedRoles.Contains(user.Role.RoleCode))
            return Result.Failure("FORBIDDEN", "Không thể xóa tài khoản này.");

        // Soft delete (deactivate)
        user.IsActive = false;
        await _uow.SaveChangesAsync(ct);
        _logger.LogWarning("Account UserId={UserId} (role={Role}) deactivated by admin", userId, user.Role.RoleCode);
        return Result.Success();
    }

    // ─── Select Lists ─────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<(int UserId, string FullName, string? ClassName)>>> GetStudentSelectListAsync(
        int schoolId, CancellationToken ct = default)
    {
        var profiles = await _uow.StudentProfiles.FindAsync(
            sp => sp.SchoolId == schoolId,
            include: q => q.Include(sp => sp.User),
            ct: ct);

        var list = profiles
            .OrderBy(sp => sp.User.FullName)
            .Select(sp => (sp.UserId, sp.User.FullName, (string?)null))
            .ToList() as IReadOnlyList<(int, string, string?)>;

        return Result<IReadOnlyList<(int, string, string?)>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<(int ClassId, string ClassName)>>> GetClassSelectListAsync(
        int schoolId, CancellationToken ct = default)
    {
        var classes = await _uow.Classes.FindAsync(
            c => c.SchoolId == schoolId && c.IsActive,
            ct: ct);

        var list = classes
            .OrderBy(c => c.ClassName)
            .Select(c => (c.Id, c.ClassName))
            .ToList() as IReadOnlyList<(int, string)>;

        return Result<IReadOnlyList<(int, string)>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<(int SubjectId, string SubjectName, string SubjectCode)>>> GetSubjectSelectListAsync(
        int schoolId, CancellationToken ct = default)
    {
        var subjects = await _uow.Subjects.FindAsync(
            s => s.SchoolId == schoolId,
            ct: ct);

        var list = subjects
            .OrderBy(s => s.SubjectName)
            .Select(s => (s.Id, s.SubjectName, s.SubjectCode))
            .ToList() as IReadOnlyList<(int, string, string)>;

        return Result<IReadOnlyList<(int, string, string)>>.Success(list);
    }

    public async Task<int?> GetTeacherPrimarySubjectIdAsync(
        int schoolId, int teacherUserId, CancellationToken ct = default)
    {
        var profiles = await _uow.TeacherProfiles.FindAsync(
            p => p.UserId == teacherUserId && p.SchoolId == schoolId, ct: ct);
        return profiles.FirstOrDefault()?.PrimarySubjectId;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static AccountListItemDto ToListItem(User u)
    {
        string? code = u.Role.RoleCode switch
        {
            "STUDENT"    => u.StudentProfile?.StudentCode,
            "TEACHER"    => u.TeacherProfile?.TeacherCode,
            "SUPERVISOR" => u.SupervisorProfile?.SupervisorCode,
            _            => null
        };
        // Prefer PrimarySubject name (FK) over free-text SpecializationSubject
        string? subject = u.TeacherProfile?.PrimarySubject?.SubjectName
                       ?? u.TeacherProfile?.SpecializationSubject;

        return new AccountListItemDto(
            UserId:      u.Id,
            FullName:    u.FullName,
            Email:       u.Email,
            PhoneNumber: u.PhoneNumber,
            AvatarUrl:   u.AvatarUrl,
            RoleCode:    u.Role.RoleCode,
            RoleName:    u.Role.RoleName,
            IsActive:    u.IsActive,
            CreatedAt:   u.CreatedAt,
            Code:        code,
            ClassName:   null,
            SubjectName: subject
        );
    }

    private async Task<AccountDetailDto> ToDetailAsync(User u, int schoolId, CancellationToken ct)
    {
        // Try to get class name for student
        string? className  = null;
        int?    classId    = null;
        if (u.Role.RoleCode == "STUDENT")
        {
            var enrollments = await _uow.ClassEnrollments.FindAsync(
                e => e.StudentId == u.Id && e.Status == EnrollmentStatus.Active,
                include: q => q.Include(e => e.Class),
                ct: ct);
            var enrollment = enrollments.FirstOrDefault();
            className = enrollment?.Class?.ClassName;
            classId   = enrollment?.ClassId;
        }

        // Parent: get linked student
        string? linkedStudentName = null;
        int?    linkedStudentId   = null;
        if (u.Role.RoleCode == "PARENT")
        {
            var relations = await _uow.ParentStudentRelations.FindAsync(
                r => r.ParentUserId == u.Id,
                include: q => q.Include(r => r.Student),
                ct: ct);
            var rel = relations.FirstOrDefault();
            linkedStudentName = rel?.Student?.FullName;
            linkedStudentId   = rel?.StudentUserId;
        }

        DateOnly? dob = u.Role.RoleCode switch
        {
            "STUDENT"    => u.StudentProfile?.DateOfBirth,
            "TEACHER"    => u.TeacherProfile?.DateOfBirth,
            "SUPERVISOR" => u.SupervisorProfile?.DateOfBirth,
            _            => null
        };

        string? code = u.Role.RoleCode switch
        {
            "STUDENT"    => u.StudentProfile?.StudentCode,
            "TEACHER"    => u.TeacherProfile?.TeacherCode,
            "SUPERVISOR" => u.SupervisorProfile?.SupervisorCode,
            _            => null
        };

        return new AccountDetailDto(
            UserId:               u.Id,
            FullName:             u.FullName,
            Email:                u.Email,
            PhoneNumber:          u.PhoneNumber,
            AvatarUrl:            u.AvatarUrl,
            RoleCode:             u.Role.RoleCode,
            RoleName:             u.Role.RoleName,
            IsActive:             u.IsActive,
            IsEmailVerified:      u.IsEmailVerified,
            LastLoginAt:          u.LastLoginAt,
            CreatedAt:            u.CreatedAt,
            Code:                 code,
            DateOfBirth:          dob,
            Gender:               u.Role.RoleCode == "STUDENT"    ? u.StudentProfile?.Gender :
                                  u.Role.RoleCode == "TEACHER"    ? u.TeacherProfile?.Gender :
                                  u.Role.RoleCode == "SUPERVISOR" ? u.SupervisorProfile?.Gender : null,
            SpecializationSubject: u.TeacherProfile?.SpecializationSubject,
            PrimarySubjectId:      u.TeacherProfile?.PrimarySubjectId,
            PrimarySubjectName:    u.TeacherProfile?.PrimarySubject?.SubjectName,
            Qualification:        u.TeacherProfile?.Qualification,
            CurrentClassId:       classId,
            ClassName:            className,
            LinkedStudentName:    linkedStudentName,
            LinkedStudentId:      linkedStudentId,
            Occupation:           u.ParentProfile?.Occupation,
            WorkAddress:          u.ParentProfile?.WorkAddress
        );
    }
}
