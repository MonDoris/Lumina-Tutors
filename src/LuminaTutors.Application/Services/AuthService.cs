using AutoMapper;
using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork               _uow;
    private readonly IMapper                   _mapper;
    private readonly IPasswordHasher<User>     _hasher;
    private readonly IConfiguration            _config;
    private readonly ILogger<AuthService>      _logger;

    public AuthService(
        IUnitOfWork uow,
        IMapper mapper,
        IPasswordHasher<User> hasher,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _hasher = hasher;
        _config = config;
        _logger = logger;
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Email == request.Email.Trim().ToLowerInvariant(),
            include: q => q.Include(u => u.Role).Include(u => u.School),
            ct: ct);

        var user = users.FirstOrDefault();

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("AUTH_INVALID", "Email hoặc mật khẩu không chính xác.");

        var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Result<LoginResponse>.Failure("AUTH_INVALID", "Email hoặc mật khẩu không chính xác.");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in at {Time}", user.Id, DateTime.UtcNow);

        var response = new LoginResponse(
            UserId:     user.Id,
            FullName:   user.FullName,
            Email:      user.Email,
            RoleCode:   user.Role.RoleCode.ToString(),
            RoleName:   user.Role.RoleName,
            SchoolId:   user.SchoolId,
            SchoolName: user.School.SchoolName,
            AvatarUrl:  user.AvatarUrl);

        return Result<LoginResponse>.Success(response);
    }

    // ─── GetCurrentUser ───────────────────────────────────────────────────────

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var users = await _uow.Users.FindAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.Role).Include(u => u.School),
            ct: ct);

        var user = users.FirstOrDefault();
        if (user is null)
            return Result<CurrentUserDto>.Failure("NOT_FOUND", "Người dùng không tồn tại.");

        var dto = new CurrentUserDto(
            UserId:    user.Id,
            FullName:  user.FullName,
            Email:     user.Email,
            RoleCode:  user.Role.RoleCode.ToString(),
            RoleName:  user.Role.RoleName,
            SchoolId:  user.SchoolId,
            SchoolName:user.School.SchoolName,
            AvatarUrl: user.AvatarUrl,
            IsActive:  user.IsActive);

        return Result<CurrentUserDto>.Success(dto);
    }

    // ─── ChangePassword ───────────────────────────────────────────────────────

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return Result.Failure("PASS_MISMATCH", "Mật khẩu xác nhận không khớp.");

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure("NOT_FOUND", "Người dùng không tồn tại.");

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            return Result.Failure("AUTH_INVALID", "Mật khẩu hiện tại không đúng.");

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ─── Logout ───────────────────────────────────────────────────────────────

    public async Task<Result> LogoutAsync(int userId, string token, CancellationToken ct = default)
    {
        // Invalidate refresh token if supplied
        if (!string.IsNullOrWhiteSpace(token))
        {
            var tokens = await _uow.RefreshTokens.FindAsync(
                t => t.UserId == userId && t.Token == token,
                ct: ct);

            var rt = tokens.FirstOrDefault();
            if (rt is not null)
            {
                rt.RevokedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation("User {UserId} logged out", userId);
        return Result.Success();
    }

    // ─── InviteLink: Create ───────────────────────────────────────────────────

    public async Task<Result<InviteLinkDto>> CreateInviteLinkAsync(
        int schoolId, int createdByUserId, CreateInviteLinkRequest request, CancellationToken ct = default)
    {
        var role = await _uow.Roles.GetByIdAsync(request.TargetRoleId, ct);
        if (role is null)
            return Result<InviteLinkDto>.Failure("NOT_FOUND", "Vai trò không hợp lệ.");

        int expiryHours = request.ExpiryHours > 0 ? request.ExpiryHours : 72;

        var invite = new InviteLink
        {
            SchoolId        = schoolId,
            Token           = Guid.NewGuid(),
            TargetRoleId    = request.TargetRoleId,
            TargetEmail     = request.TargetEmail?.Trim().ToLowerInvariant(),
            LinkedStudentId = request.LinkedStudentId,
            ExpiresAt       = DateTime.UtcNow.AddHours(expiryHours),
            CreatedByUserId = createdByUserId,
            IsRevoked       = false
        };

        await _uow.InviteLinks.AddAsync(invite, ct);
        await _uow.SaveChangesAsync(ct);

        // Reload with navigation
        var invites = await _uow.InviteLinks.FindAsync(
            i => i.Id == invite.Id,
            include: q => q.Include(i => i.TargetRole)
                           .Include(i => i.LinkedStudent!),
            ct: ct);

        var dto = MapInviteLinkDto(invites.First());
        return Result<InviteLinkDto>.Success(dto);
    }

    // ─── InviteLink: GetByToken ───────────────────────────────────────────────

    public async Task<Result<InviteLinkDto>> GetInviteLinkByTokenAsync(Guid token, CancellationToken ct = default)
    {
        var invites = await _uow.InviteLinks.FindAsync(
            i => i.Token == token,
            include: q => q.Include(i => i.TargetRole)
                           .Include(i => i.LinkedStudent!),
            ct: ct);

        var invite = invites.FirstOrDefault();
        if (invite is null)
            return Result<InviteLinkDto>.Failure("NOT_FOUND", "Liên kết không tồn tại.");

        if (!invite.IsValid)
            return Result<InviteLinkDto>.Failure("INVITE_INVALID", "Liên kết đã hết hạn hoặc đã được sử dụng.");

        return Result<InviteLinkDto>.Success(MapInviteLinkDto(invite));
    }

    // ─── InviteLink: Activate ─────────────────────────────────────────────────

    public async Task<Result<LoginResponse>> ActivateInviteAsync(ActivateInviteRequest request, CancellationToken ct = default)
    {
        if (request.Password != request.ConfirmPassword)
            return Result<LoginResponse>.Failure("PASS_MISMATCH", "Mật khẩu xác nhận không khớp.");

        var invites = await _uow.InviteLinks.FindAsync(
            i => i.Token == request.Token,
            include: q => q.Include(i => i.TargetRole)
                           .Include(i => i.School),
            ct: ct);

        var invite = invites.FirstOrDefault();
        if (invite is null || !invite.IsValid)
            return Result<LoginResponse>.Failure("INVITE_INVALID", "Liên kết không hợp lệ hoặc đã hết hạn.");

        // Check email uniqueness within school
        if (!string.IsNullOrWhiteSpace(invite.TargetEmail))
        {
            var existing = await _uow.Users.FindAsync(
                u => u.SchoolId == invite.SchoolId && u.Email == invite.TargetEmail,
                ct: ct);

            if (existing.Any())
                return Result<LoginResponse>.Failure("EMAIL_EXISTS", "Email đã được sử dụng trong trường này.");
        }

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var newUser = new User
            {
                SchoolId        = invite.SchoolId,
                RoleId          = invite.TargetRoleId,
                Email           = invite.TargetEmail ?? $"user_{invite.Token:N}@lumina.local",
                FullName        = request.FullName.Trim(),
                PhoneNumber     = request.PhoneNumber.Trim(),
                IsActive        = true,
                IsEmailVerified = true,
                PasswordHash    = string.Empty
            };

            // Hash password using temp instance
            newUser.PasswordHash = _hasher.HashPassword(newUser, request.Password);

            await _uow.Users.AddAsync(newUser, ct);
            await _uow.SaveChangesAsync(ct);

            // Mark invite as used
            invite.UsedAt       = DateTime.UtcNow;
            invite.UsedByUserId = newUser.Id;
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Invite {Token} activated → User {UserId}", request.Token, newUser.Id);

            var loginResponse = new LoginResponse(
                UserId:     newUser.Id,
                FullName:   newUser.FullName,
                Email:      newUser.Email,
                RoleCode:   invite.TargetRole.RoleCode.ToString(),
                RoleName:   invite.TargetRole.RoleName,
                SchoolId:   invite.SchoolId,
                SchoolName: invite.School.SchoolName,
                AvatarUrl:  null);

            return Result<LoginResponse>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "ActivateInvite failed for token {Token}", request.Token);
            return Result<LoginResponse>.Failure("INTERNAL_ERROR", "Có lỗi xảy ra. Vui lòng thử lại.");
        }
    }

    // ─── InviteLink: GetList ──────────────────────────────────────────────────

    public async Task<Result<PagedResult<InviteLinkDto>>> GetInviteLinksAsync(
        int schoolId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var paged = await _uow.InviteLinks.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize:   pageSize,
            filter:     i => i.SchoolId == schoolId,
            orderBy:    q => q.OrderByDescending(i => i.CreatedAt),
            include:    q => q.Include(i => i.TargetRole)
                              .Include(i => i.LinkedStudent!),
            ct: ct);

        var dtos = paged.Items.Select(MapInviteLinkDto).ToList();
        var result = PagedResult<InviteLinkDto>.Create(dtos, paged.TotalCount, pageNumber, pageSize);
        return Result<PagedResult<InviteLinkDto>>.Success(result);
    }

    // ─── InviteLink: Revoke ───────────────────────────────────────────────────

    public async Task<Result> RevokeInviteLinkAsync(int inviteId, int revokedByUserId, CancellationToken ct = default)
    {
        var invite = await _uow.InviteLinks.GetByIdAsync(inviteId, ct);
        if (invite is null)
            return Result.Failure("NOT_FOUND", "Liên kết không tồn tại.");

        if (invite.IsUsed)
            return Result.Failure("INVITE_USED", "Liên kết đã được sử dụng, không thể thu hồi.");

        invite.IsRevoked = true;
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("InviteLink {Id} revoked by User {UserId}", inviteId, revokedByUserId);
        return Result.Success();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static InviteLinkDto MapInviteLinkDto(InviteLink invite) => new(
        InviteId:          invite.Id,
        Token:             invite.Token,
        TargetRoleName:    invite.TargetRole?.RoleName ?? string.Empty,
        TargetEmail:       invite.TargetEmail,
        LinkedStudentName: invite.LinkedStudent?.FullName,
        ExpiresAt:         invite.ExpiresAt,
        IsValid:           invite.IsValid,
        CreatedAt:         invite.CreatedAt);
}
