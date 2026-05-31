using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Domain.Entities.Academic;

public class Semester : BaseEntity
{
    public int AcademicYearId { get; set; }
    public int SchoolId { get; set; }
    public byte SemesterNumber { get; set; }     // 1 or 2
    public string SemesterName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = false;

    // Navigation
    public AcademicYear AcademicYear { get; set; } = null!;
    public School School { get; set; } = null!;
    public ICollection<SubjectAssignment> SubjectAssignments { get; set; } = [];
}
