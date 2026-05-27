using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.HR;

public record TeacherDetailDto(
    int      UserId,
    string   TeacherCode,
    string   FullName,
    string   Email,
    string?  PhoneNumber,
    string?  AvatarUrl,
    DateOnly? DateOfBirth,
    string?  Gender,
    string?  Qualification,
    string?  SpecializationSubject,
    DateOnly? HireDate,
    string?  ContractType,
    string?  BankName,
    bool     IsActive,
    List<string> AssignedSubjects,
    List<string> AssignedClasses
);

public record CreateTeacherRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress]   string Email,
    [Required, MaxLength(30)]  string TeacherCode,
    [Phone]   string?  PhoneNumber,
    DateOnly? DateOfBirth,
    Gender?   Gender,
    string?   Qualification,
    string?   SpecializationSubject,
    DateOnly? HireDate,
    ContractType ContractType = ContractType.FullTime,
    string?   BankAccountNumber = null,
    string?   BankName         = null,
    string?   TaxCode          = null
);

public record CreateContractRequest(
    [Required] int          TeacherId,
    [Required, MaxLength(50)] string ContractCode,
    [Required] ContractType ContractType,
    [Required] DateOnly     StartDate,
    DateOnly?  EndDate,
    [Required, Range(1_000_000, 100_000_000)] decimal BaseSalary,
    DateOnly?  SignedAt,
    string?    DocumentUrl
);

public record PayrollDto(
    int     PayrollId,
    string  TeacherName,
    byte    Month,
    short   Year,
    decimal BaseSalary,
    decimal TeachingAllowance,
    decimal PositionAllowance,
    decimal OvertimePay,
    decimal Bonus,
    decimal GrossIncome,
    decimal InsuranceDeduction,
    decimal TaxDeduction,
    decimal OtherDeductions,
    decimal NetSalary,
    string  Status
);

public record CreatePayrollRequest(
    [Required] int   TeacherId,
    [Required] byte  Month,
    [Required] short Year,
    [Required] decimal BaseSalary,
    decimal TeachingAllowance  = 0,
    decimal PositionAllowance  = 0,
    decimal OvertimePay        = 0,
    decimal Bonus              = 0,
    decimal InsuranceDeduction = 0,
    decimal TaxDeduction       = 0,
    decimal OtherDeductions    = 0,
    string? Note               = null
);
