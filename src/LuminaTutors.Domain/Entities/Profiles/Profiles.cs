using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Profiles;

// ─── Student Profile ──────────────────────────────────────────────────────────

public class StudentProfile : BaseEntity
{
    public int UserId { get; set; }
    public int SchoolId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EthnicGroup { get; set; }
    public DateOnly? AdmissionDate { get; set; }
    public AdmissionType? AdmissionType { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}

// ─── Teacher Profile ──────────────────────────────────────────────────────────

public class TeacherProfile : BaseEntity
{
    public int UserId { get; set; }
    public int SchoolId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Qualification { get; set; }
    /// <summary>Free-text label kept for display (e.g. "Toán học"). PrimarySubjectId is the authoritative FK.</summary>
    public string? SpecializationSubject { get; set; }
    /// <summary>FK → Subject. Drives auto-population of subject pickers in Lab / Quiz / Homework forms.</summary>
    public int? PrimarySubjectId { get; set; }
    public DateOnly? HireDate { get; set; }
    public ContractType? ContractType { get; set; }
    public string? TaxCode { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
    public LuminaTutors.Domain.Entities.Academic.Subject? PrimarySubject { get; set; }
}

// ─── Parent Profile ───────────────────────────────────────────────────────────

public class ParentProfile : BaseEntity
{
    public int UserId { get; set; }
    public int SchoolId { get; set; }
    public string? Occupation { get; set; }
    public string? WorkAddress { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}

// ─── Parent-Student Many-to-Many ──────────────────────────────────────────────

public class ParentStudentRelation : BaseEntity
{
    public int ParentUserId { get; set; }
    public int StudentUserId { get; set; }
    public string Relationship { get; set; } = string.Empty; // Cha | Mẹ | Người giám hộ
    public bool IsPrimaryContact { get; set; } = false;

    public User Parent { get; set; } = null!;
    public User Student { get; set; } = null!;
}

// ─── Supervisor Profile ───────────────────────────────────────────────────────

public class SupervisorProfile : BaseEntity
{
    public int UserId { get; set; }
    public int SchoolId { get; set; }
    public string SupervisorCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? HireDate { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}

// ─── Accountant Profile ───────────────────────────────────────────────────────

public class AccountantProfile : BaseEntity
{
    public int UserId { get; set; }
    public int SchoolId { get; set; }
    public string AccountantCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? HireDate { get; set; }

    public User User { get; set; } = null!;
    public School School { get; set; } = null!;
}
