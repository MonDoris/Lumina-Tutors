using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Student;

// ─── List / Summary ──────────────────────────────────────────────────────────

public record StudentSummaryDto(
    int     UserId,
    string  StudentCode,
    string  FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? ClassName,
    string? GradeName,
    bool    IsActive
);

// ─── Detail ──────────────────────────────────────────────────────────────────

public record StudentDetailDto(
    int      UserId,
    string   StudentCode,
    string   FullName,
    string   Email,
    string?  PhoneNumber,
    string?  AvatarUrl,
    DateOnly? DateOfBirth,
    string?  Gender,
    string?  PlaceOfBirth,
    string?  PermanentAddress,
    string?  EthnicGroup,
    DateOnly? AdmissionDate,
    string?  AdmissionType,
    // Current class
    int?     CurrentClassId,
    string?  CurrentClassName,
    string?  CurrentGradeName,
    string?  HomeRoomTeacherName,
    // Parents
    List<ParentInfoDto> Parents,
    bool IsActive
);

public record ParentInfoDto(
    int    ParentUserId,
    string FullName,
    string? PhoneNumber,
    string Relationship,
    bool   IsPrimaryContact
);

// ─── Create / Update ─────────────────────────────────────────────────────────

public record CreateStudentRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress]   string Email,
    [Required, MaxLength(30)]  string StudentCode,
    DateOnly? DateOfBirth,
    Gender?  Gender,
    string?  PlaceOfBirth,
    string?  PermanentAddress,
    string?  EthnicGroup,
    DateOnly? AdmissionDate,
    AdmissionType AdmissionType = AdmissionType.New,
    int?     InitialClassId = null
);

public record UpdateStudentRequest(
    [Required, MaxLength(150)] string FullName,
    [Phone]  string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender?  Gender,
    string?  PlaceOfBirth,
    string?  PermanentAddress,
    string?  EthnicGroup
);

// ─── Enrollment ──────────────────────────────────────────────────────────────

public record EnrollStudentRequest(
    [Required] int ClassId,
    DateOnly?  EnrolledDate
);

public record TransferStudentRequest(
    [Required] int    NewClassId,
    [Required, MaxLength(500)] string TransferNote
);

// ─── Search / Filter ─────────────────────────────────────────────────────────

public record StudentSearchRequest(
    string?  Keyword,
    int?     ClassId,
    int?     GradeLevelId,
    bool?    IsActive,
    int      PageNumber = 1,
    int      PageSize   = 20
);
