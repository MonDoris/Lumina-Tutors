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
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), MaxLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự.")] string FullName,
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]   string Email,
    [Required(ErrorMessage = "Vui lòng nhập mã học sinh."), MaxLength(30, ErrorMessage = "Mã học sinh tối đa 30 ký tự.")]  string StudentCode,
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
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), MaxLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự.")] string FullName,
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]  string? PhoneNumber,
    DateOnly? DateOfBirth,
    Gender?  Gender,
    string?  PlaceOfBirth,
    string?  PermanentAddress,
    string?  EthnicGroup
);

// ─── Enrollment ──────────────────────────────────────────────────────────────

public record EnrollStudentRequest(
    [Required(ErrorMessage = "Vui lòng chọn lớp học.")] int ClassId,
    DateOnly?  EnrolledDate
);

public record TransferStudentRequest(
    [Required(ErrorMessage = "Vui lòng chọn lớp mới.")] int    NewClassId,
    [Required(ErrorMessage = "Vui lòng nhập lý do chuyển lớp."), MaxLength(500, ErrorMessage = "Lý do chuyển lớp tối đa 500 ký tự.")] string TransferNote
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
