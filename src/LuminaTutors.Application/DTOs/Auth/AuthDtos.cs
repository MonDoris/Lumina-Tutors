using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Auth;

// ─── Requests ────────────────────────────────────────────────────────────────

public record LoginRequest(
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email."),
     EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ."),
     MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")] string Email,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."),
     MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự."),
     MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")] string Password,
    bool RememberMe = false
);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")] string CurrentPassword,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới."),
     MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")] string NewPassword,
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")] string ConfirmNewPassword
);

public record ActivateInviteRequest(
    [Required(ErrorMessage = "Token không hợp lệ.")] Guid Token,
    [Required(ErrorMessage = "Vui lòng nhập họ và tên."),
     MinLength(6, ErrorMessage = "Họ tên tối thiểu 6 ký tự.")] string FullName,
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."),
     Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] string PhoneNumber,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."),
     MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")] string Password,
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")] string ConfirmPassword
);

public record ForgotPasswordRequest(
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email."),
     EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")] string Email
);

public record ResetPasswordRequest(
    [Required(ErrorMessage = "Token đặt lại mật khẩu không hợp lệ.")] string ResetToken,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới."),
     MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")] string NewPassword,
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")] string ConfirmPassword
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
