using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.HR;

public class TeacherDetailDto
{
    public int       ProfileId             { get; init; }
    public int       UserId                { get; init; }
    public string    TeacherCode           { get; init; } = "";
    public string    FullName              { get; init; } = "";
    public string    Email                 { get; init; } = "";
    public string?   PhoneNumber           { get; init; }
    public string?   AvatarUrl             { get; init; }
    public DateOnly? DateOfBirth           { get; init; }
    public string?   Gender                { get; init; }
    public string?   Qualification         { get; init; }
    public string?   SpecializationSubject { get; init; }
    public DateOnly? HireDate              { get; init; }
    public string?   ContractType          { get; init; }
    public string?   BankName              { get; init; }
    public bool      IsActive              { get; init; }
    public List<string> AssignedSubjects   { get; init; } = new();
    public List<string> AssignedClasses    { get; init; } = new();
}

public record CreateTeacherRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), MaxLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự.")] string FullName,
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]   string Email,
    [Required(ErrorMessage = "Vui lòng nhập mã giáo viên."), MaxLength(30, ErrorMessage = "Mã giáo viên tối đa 30 ký tự.")]  string TeacherCode,
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]   string?  PhoneNumber,
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
    [Required(ErrorMessage = "Vui lòng chọn giáo viên.")] int          TeacherId,
    [Required(ErrorMessage = "Vui lòng nhập mã hợp đồng."), MaxLength(50, ErrorMessage = "Mã hợp đồng tối đa 50 ký tự.")] string ContractCode,
    [Required(ErrorMessage = "Vui lòng chọn loại hợp đồng.")] ContractType ContractType,
    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")] DateOnly     StartDate,
    DateOnly?  EndDate,
    [Required(ErrorMessage = "Vui lòng nhập lương cơ bản."), Range(1_000_000, 100_000_000, ErrorMessage = "Lương cơ bản phải từ 1.000.000 đến 100.000.000.")] decimal BaseSalary,
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
    [Required(ErrorMessage = "Vui lòng chọn giáo viên.")] int   TeacherId,
    [Required(ErrorMessage = "Vui lòng nhập tháng.")] byte  Month,
    [Required(ErrorMessage = "Vui lòng nhập năm.")] short Year,
    [Required(ErrorMessage = "Vui lòng nhập lương cơ bản.")] decimal BaseSalary,
    decimal TeachingAllowance  = 0,
    decimal PositionAllowance  = 0,
    decimal OvertimePay        = 0,
    decimal Bonus              = 0,
    decimal InsuranceDeduction = 0,
    decimal TaxDeduction       = 0,
    decimal OtherDeductions    = 0,
    string? Note               = null
);
