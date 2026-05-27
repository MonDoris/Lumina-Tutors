using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Academic;

public class Subject : BaseEntity
{
    public int SchoolId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public SubjectCategory SubjectCategory { get; set; } = SubjectCategory.Main;
    public bool Has3DLab { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation
    public School School { get; set; } = null!;
    public ICollection<SubjectAssignment> SubjectAssignments { get; set; } = [];
}
