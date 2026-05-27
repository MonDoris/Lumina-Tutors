using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.HR;

// ─── TeacherContract ──────────────────────────────────────────────────────────

public class TeacherContract : AuditableEntity
{
    public int SchoolId { get; set; }
    public int TeacherId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public ContractType ContractType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }          // null = indefinite
    public decimal BaseSalary { get; set; }
    public DateOnly? SignedAt { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public string? DocumentUrl { get; set; }
    public int CreatedByUserId { get; set; }

    public School School { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}

// ─── StaffAttendance (Chấm công nhân viên) ───────────────────────────────────

public class StaffAttendance : BaseEntity
{
    public int SchoolId { get; set; }
    public int UserId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public StaffAttendanceStatus Status { get; set; } = StaffAttendanceStatus.Present;
    public string? Note { get; set; }

    // Computed property (not persisted in EF — calculate in service layer)
    public double? WorkingHours =>
        CheckInTime.HasValue && CheckOutTime.HasValue
            ? (CheckOutTime.Value.ToTimeSpan() - CheckInTime.Value.ToTimeSpan()).TotalHours
            : null;

    public School School { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ─── Payroll (Bảng lương tháng) ───────────────────────────────────────────────

public class Payroll : AuditableEntity
{
    public int SchoolId { get; set; }
    public int UserId { get; set; }
    public byte PayrollMonth { get; set; }          // 1-12
    public short PayrollYear { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal TeachingAllowance { get; set; } = 0M;
    public decimal PositionAllowance { get; set; } = 0M;
    public decimal OvertimePay { get; set; } = 0M;
    public decimal Bonus { get; set; } = 0M;
    public decimal InsuranceDeduction { get; set; } = 0M;  // BHXH + BHYT + BHTN
    public decimal TaxDeduction { get; set; } = 0M;         // Thuế TNCN
    public decimal OtherDeductions { get; set; } = 0M;

    // Computed in application layer
    public decimal GrossIncome => BaseSalary + TeachingAllowance + PositionAllowance + OvertimePay + Bonus;
    public decimal NetSalary => GrossIncome - InsuranceDeduction - TaxDeduction - OtherDeductions;

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }

    public School School { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? ApprovedBy { get; set; }
}

// ─── TeacherEvaluation (Đánh giá 360°) ───────────────────────────────────────

public class TeacherEvaluation : BaseEntity
{
    public int SchoolId { get; set; }
    public int TeacherId { get; set; }
    public int EvaluatorId { get; set; }
    public EvaluatorRole EvaluatorRole { get; set; }
    public int AcademicYearId { get; set; }
    public byte? TeachingScore { get; set; }        // 1-5
    public byte? ProfessionalScore { get; set; }
    public byte? AttitudeScore { get; set; }
    public byte? OverallScore { get; set; }
    public string? Comments { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public User Evaluator { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
}
