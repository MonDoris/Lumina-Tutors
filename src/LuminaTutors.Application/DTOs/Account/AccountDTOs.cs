using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Account;

// ─── List / Summary ───────────────────────────────────────────────────────────

public record AccountListItemDto(
    int     UserId,
    string  FullName,
    string  Email,
    string? PhoneNumber,
    string? AvatarUrl,
    string  RoleCode,
    string  RoleName,
    bool    IsActive,
    DateTime CreatedAt,
    // Role-specific extras
    string? Code,            // StudentCode / TeacherCode / SupervisorCode
    string? ClassName,       // For Student (current class)
    string? SubjectName      // For Teacher (specialization)
);

// ─── Detail ───────────────────────────────────────────────────────────────────

public record AccountDetailDto(
    int     UserId,
    string  FullName,
    string  Email,
    string? PhoneNumber,
    string? AvatarUrl,
    string  RoleCode,
    string  RoleName,
    bool    IsActive,
    bool    IsEmailVerified,
    DateTime? LastLoginAt,
    DateTime  CreatedAt,
    // Common profile
    string?  Code,
    DateOnly? DateOfBirth,
    Gender?  Gender,
    // Teacher-specific
    string?  SpecializationSubject,
    string?  Qualification,
    // Student-specific
    int?     CurrentClassId,
    string?  ClassName,
    // Parent-specific
    string?  LinkedStudentName,
    int?     LinkedStudentId,
    string?  Occupation,
    string?  WorkAddress
);

// ─── Create ───────────────────────────────────────────────────────────────────

public record CreateAccountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public string RoleCode { get; init; } = string.Empty; // STUDENT | TEACHER | SUPERVISOR | PARENT

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public Gender? Gender { get; init; }

    [Required(ErrorMessage = "Vui lòng đặt mật khẩu")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    public string Password { get; init; } = string.Empty;

    // Teacher-specific
    public string? SpecializationSubject { get; init; }
    public string? Qualification { get; init; }

    // Student-specific
    public int? ClassId { get; init; }

    // Parent-specific (standalone parent account)
    public int? LinkedStudentUserId { get; init; }
    public string? Relationship { get; init; } // Cha | Mẹ | Người giám hộ
    public string? Occupation { get; init; }
    public string? WorkAddress { get; init; }

    // Auto-create linked parent when creating STUDENT
    public bool    CreateLinkedParent    { get; init; } = false;
    public string? ParentFullName        { get; init; }
    public string? ParentPhoneNumber     { get; init; }
    public string? ParentEmail           { get; init; }
    public string? ParentPassword        { get; init; }
    public string? ParentRelationship    { get; init; } = "Phụ huynh";

    // Avatar: handled separately via IFormFile in controller
    public string? AvatarUrl { get; init; }
}

// ─── Update ───────────────────────────────────────────────────────────────────

public record UpdateAccountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public Gender? Gender { get; init; }

    public bool IsActive { get; init; } = true;

    // Teacher-specific
    public string? SpecializationSubject { get; init; }
    public string? Qualification { get; init; }

    // Student: reassign class
    public int? ClassId { get; init; }

    // Parent: relink student + profile
    public int? LinkedStudentUserId { get; init; }
    public string? Relationship { get; init; }
    public string? Occupation { get; init; }
    public string? WorkAddress { get; init; }

    // Avatar: handled separately via IFormFile in controller
    public string? AvatarUrl { get; init; }
}

// ─── Reset Password ───────────────────────────────────────────────────────────

public record AdminResetPasswordRequest(
    [Required, MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    string NewPassword,
    [Required]
    string ConfirmPassword
);

// ─── Filter / Paging ─────────────────────────────────────────────────────────

public record AccountFilterRequest(
    string? RoleCode  = null,
    string? Keyword   = null,
    bool?   IsActive  = null,
    int     Page      = 1,
    int     PageSize  = 20
);
