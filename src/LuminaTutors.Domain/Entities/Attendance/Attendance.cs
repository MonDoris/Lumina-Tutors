using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Attendance;

// ─── Schedule (TKB) ───────────────────────────────────────────────────────────

public class Schedule : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SemesterId { get; set; }
    public int SubjectAssignmentId { get; set; }
    public byte DayOfWeek { get; set; }       // 2=Mon .. 7=Sat
    public byte PeriodStart { get; set; }
    public byte PeriodEnd { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? RoomOverride { get; set; }
    public bool IsActive { get; set; } = true;

    public School School { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public SubjectAssignment SubjectAssignment { get; set; } = null!;
    public ICollection<ScheduleChangeLog> ChangeLogs { get; set; } = [];
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = [];
}

// ─── ScheduleChangeLog ────────────────────────────────────────────────────────

public class ScheduleChangeLog : BaseEntity
{
    public int ScheduleId { get; set; }
    public int ChangedByUserId { get; set; }
    public ScheduleChangeType ChangeType { get; set; }
    public byte? OldDayOfWeek { get; set; }
    public byte? OldPeriodStart { get; set; }
    public string? OldRoomNumber { get; set; }
    public byte? NewDayOfWeek { get; set; }
    public byte? NewPeriodStart { get; set; }
    public string? NewRoomNumber { get; set; }
    public string? Reason { get; set; }
    public DateOnly? AppliedDate { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Schedule Schedule { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}

// ─── AttendanceSession (QR Model A) ──────────────────────────────────────────

public class AttendanceSession : AuditableEntity
{
    public int SchoolId { get; set; }
    public int ScheduleId { get; set; }
    public DateOnly SessionDate { get; set; }
    public Guid QRToken { get; set; } = Guid.NewGuid();
    public DateTime QRExpiresAt { get; set; }
    public int CreatedByTeacherId { get; set; }
    public SessionStatus SessionStatus { get; set; } = SessionStatus.Open;
    public string? TopicNote { get; set; }
    public DateTime? ClosedAt { get; set; }

    public School School { get; set; } = null!;
    public Schedule Schedule { get; set; } = null!;
    public User CreatedByTeacher { get; set; } = null!;
    public ICollection<StudentAttendance> Attendances { get; set; } = [];
}

// ─── StudentAttendance ────────────────────────────────────────────────────────

public class StudentAttendance : BaseEntity
{
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public DateTime? CheckedInAt { get; set; }
    public CheckMethod? CheckMethod { get; set; }
    public string? Note { get; set; }
    public bool NotifiedParent { get; set; } = false;
    public DateTime? NotifiedAt { get; set; }
    public int? UpdatedByTeacherId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AttendanceSession Session { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User? UpdatedByTeacher { get; set; }
}

// ─── SchoolEvent ──────────────────────────────────────────────────────────────

public class SchoolEvent : AuditableEntity
{
    public int SchoolId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int OrganizedByUserId { get; set; }

    public School School { get; set; } = null!;
    public User OrganizedBy { get; set; } = null!;
    public ICollection<EventAttendance> EventAttendances { get; set; } = [];
}

// ─── EventAttendance ──────────────────────────────────────────────────────────

public class EventAttendance : BaseEntity
{
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public int? CheckedByUserId { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string? Note { get; set; }

    public SchoolEvent Event { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User? CheckedBy { get; set; }
}
