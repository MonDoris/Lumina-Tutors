using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Grading;

// ─── GradeCategory (Loại điểm TT22) ─────────────────────────────────────────

public class GradeCategory : BaseEntity
{
    public string CategoryCode { get; set; } = string.Empty;   // DTX | DGK | DCK
    public string CategoryName { get; set; } = string.Empty;
    public byte Coefficient { get; set; }                      // 1 | 2 | 3
    public byte? MaxCountPerSemester { get; set; }             // null = unlimited
    public bool IsMultipleAllowed { get; set; }

    public ICollection<ScoreEntry> ScoreEntries { get; set; } = [];
}

// ─── SubjectGradeRequirement ──────────────────────────────────────────────────

public class SubjectGradeRequirement : BaseEntity
{
    public int SchoolId { get; set; }
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public int GradeCategoryId { get; set; }
    public byte MinCount { get; set; }

    public School School { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
    public GradeCategory GradeCategory { get; set; } = null!;
}

// ─── ScoreEntry (Từng đầu điểm thô) ─────────────────────────────────────────

public class ScoreEntry : AuditableEntity
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int SubjectAssignmentId { get; set; }
    public int GradeCategoryId { get; set; }
    public byte EntryOrder { get; set; } = 1;    // ĐTX1, ĐTX2, ĐTX3 ...
    public decimal Score { get; set; }            // 0.00 – 10.00
    public DateOnly? ExamDate { get; set; }
    public string? Note { get; set; }
    public int EnteredByTeacherId { get; set; }
    public bool IsLocked { get; set; } = false;

    public School School { get; set; } = null!;
    public User Student { get; set; } = null!;
    public SubjectAssignment SubjectAssignment { get; set; } = null!;
    public GradeCategory GradeCategory { get; set; } = null!;
    public User EnteredByTeacher { get; set; } = null!;
}

// ─── GradeBook (Bảng điểm tổng kết) ─────────────────────────────────────────

public class GradeBook : BaseEntity
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int SubjectAssignmentId { get; set; }
    public decimal? AverageScore { get; set; }       // ĐTBm — computed by SP
    public string? LetterGrade { get; set; }
    public GradeRemark? Remark { get; set; }
    public bool IsCalculated { get; set; } = false;
    public DateTime? CalculatedAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public School School { get; set; } = null!;
    public User Student { get; set; } = null!;
    public SubjectAssignment SubjectAssignment { get; set; } = null!;
    public User? ApprovedBy { get; set; }
}

// ─── Exam ────────────────────────────────────────────────────────────────────

public class Exam : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public ExamType ExamType { get; set; }
    public DateOnly ExamDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public short DurationMinutes { get; set; }
    public decimal MaxScore { get; set; } = 10M;
    public int CreatedByUserId { get; set; }

    public School School { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<ExamRoom> ExamRooms { get; set; } = [];
}

// ─── ExamRoom ─────────────────────────────────────────────────────────────────

public class ExamRoom : BaseEntity
{
    public int ExamId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public byte Capacity { get; set; } = 30;
    public int SupervisorId { get; set; }
    public int? AssistantId { get; set; }

    public Exam Exam { get; set; } = null!;
    public User Supervisor { get; set; } = null!;
    public User? Assistant { get; set; }
    public ICollection<ExamRoomAssignment> SeatAssignments { get; set; } = [];
}

// ─── ExamRoomAssignment ───────────────────────────────────────────────────────

public class ExamRoomAssignment : BaseEntity
{
    public int ExamRoomId { get; set; }
    public int StudentId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;

    public ExamRoom ExamRoom { get; set; } = null!;
    public User Student { get; set; } = null!;
}
