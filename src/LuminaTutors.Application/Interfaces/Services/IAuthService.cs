using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result> LogoutAsync(int userId, string token, CancellationToken ct = default);

    // Invite Link flow
    Task<Result<InviteLinkDto>> CreateInviteLinkAsync(int schoolId, int createdByUserId, CreateInviteLinkRequest request, CancellationToken ct = default);
    Task<Result<InviteLinkDto>> GetInviteLinkByTokenAsync(Guid token, CancellationToken ct = default);
    Task<Result<LoginResponse>> ActivateInviteAsync(ActivateInviteRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<InviteLinkDto>>> GetInviteLinksAsync(int schoolId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<Result> RevokeInviteLinkAsync(int inviteId, int revokedByUserId, CancellationToken ct = default);
}
