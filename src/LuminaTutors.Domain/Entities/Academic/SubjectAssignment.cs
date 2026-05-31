using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Entities.Grading;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Learning;

namespace LuminaTutors.Domain.Entities.Academic;

/// <summary>
/// Phân công dạy: Teacher ↔ Subject ↔ Class ↔ Semester.
/// Hub entity — most learning/grading records reference this.
/// </summary>
public class SubjectAssignment : AuditableEntity
{
    public int SchoolId { get; set; }
    public int SemesterId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public byte PeriodsPerWeek { get; set; } = 2;

    // Navigation
    public School School { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<Lesson> Lessons { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<ScoreEntry> ScoreEntries { get; set; } = [];
    public ICollection<GradeBook> GradeBooks { get; set; } = [];
}
