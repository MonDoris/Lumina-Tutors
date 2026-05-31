using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Discipline;

// ─── DisciplineRecord ─────────────────────────────────────────────────────────

public class DisciplineRecord : AuditableEntity
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int ReportedByUserId { get; set; }
    public DateOnly RecordDate { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Minor;
    public string? Description { get; set; }
    public string? ActionTaken { get; set; }
    public DisciplineStatus Status { get; set; } = DisciplineStatus.Open;
    public int? EscalatedToUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public School School { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User ReportedBy { get; set; } = null!;
    public User? EscalatedTo { get; set; }
}

// ─── GateCheckLog ─────────────────────────────────────────────────────────────

public class GateCheckLog : BaseEntity
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public GateCheckType CheckType { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public int? CheckedByUserId { get; set; }
    public string? Note { get; set; }
    public bool IsLate { get; set; } = false;

    public School School { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User? CheckedBy { get; set; }
}
