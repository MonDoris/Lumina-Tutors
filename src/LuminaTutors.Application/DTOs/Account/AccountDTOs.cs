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
    int?     PrimarySubjectId,
    string?  PrimarySubjectName,
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

public class CreateAccountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public string RoleCode { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [Required(ErrorMessage = "Vui lòng đặt mật khẩu")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    public string Password { get; set; } = string.Empty;

    // Teacher-specific
    public string? SpecializationSubject { get; set; }
    public int?    PrimarySubjectId      { get; set; }
    public string? Qualification         { get; set; }

    // Student-specific
    public int? ClassId { get; set; }

    // Parent-specific
    public int? LinkedStudentUserId { get; set; }
    public string? Relationship { get; set; }
    public string? Occupation { get; set; }
    public string? WorkAddress { get; set; }

    // Auto-create linked parent when creating STUDENT
    public bool    CreateLinkedParent    { get; set; } = false;
    public string? ParentFullName        { get; set; }
    public string? ParentPhoneNumber     { get; set; }
    public string? ParentEmail           { get; set; }
    public string? ParentPassword        { get; set; }
    public string? ParentRelationship    { get; set; } = "Phụ huynh";

    // Avatar: handled separately via IFormFile in controller
    public string? AvatarUrl { get; set; }
}

// ─── Update ───────────────────────────────────────────────────────────────────

public class UpdateAccountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    public bool IsActive { get; set; } = true;

    // Teacher-specific
    public string? SpecializationSubject { get; set; }
    public int?    PrimarySubjectId      { get; set; }
    public string? Qualification         { get; set; }

    // Student: reassign class
    public int? ClassId { get; set; }

    // Parent: relink student + profile
    public int? LinkedStudentUserId { get; set; }
    public string? Relationship { get; set; }
    public string? Occupation { get; set; }
    public string? WorkAddress { get; set; }

    // Avatar: handled separately via IFormFile in controller
    public string? AvatarUrl { get; set; }
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
