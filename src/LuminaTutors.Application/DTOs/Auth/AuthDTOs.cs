using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Auth;

// ─── Requests ────────────────────────────────────────────────────────────────

public record LoginRequest(
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password,
    bool RememberMe = false
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword,
    [Required] string ConfirmNewPassword
);

public record ActivateInviteRequest(
    [Required] Guid Token,
    [Required, MinLength(6)] string FullName,
    [Required, Phone]        string PhoneNumber,
    [Required, MinLength(8)] string Password,
    [Required]               string ConfirmPassword
);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required] string ResetToken,
    [Required, MinLength(8)] string NewPassword,
    [Required] string ConfirmPassword
);

// ─── Responses ───────────────────────────────────────────────────────────────

public record LoginResponse(
    int    UserId,
    string FullName,
    string Email,
    string RoleCode,
    string RoleName,
    int    SchoolId,
    string SchoolName,
    string? AvatarUrl
);

public record CurrentUserDto(
    int    UserId,
    string FullName,
    string Email,
    string RoleCode,
    string RoleName,
    int    SchoolId,
    string SchoolName,
    string? AvatarUrl,
    bool   IsActive
);

public record InviteLinkDto(
    int      InviteId,
    Guid     Token,
    string   TargetRoleName,
    string?  TargetEmail,
    string?  LinkedStudentName,
    DateTime ExpiresAt,
    bool     IsValid,
    DateTime CreatedAt
);

public record CreateInviteLinkRequest(
    [Required] int     TargetRoleId,
    [EmailAddress]     string? TargetEmail,
    int?               LinkedStudentId,
    int                ExpiryHours = 72
);
