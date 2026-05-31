using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Domain.Entities.Academic;

public class AcademicYear : AuditableEntity
{
    public int SchoolId { get; set; }
    public string YearName { get; set; } = string.Empty;   // e.g. "2024-2025"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = false;

    // Navigation
    public School School { get; set; } = null!;
    public ICollection<Semester> Semesters { get; set; } = [];
    public ICollection<Class> Classes { get; set; } = [];
}
