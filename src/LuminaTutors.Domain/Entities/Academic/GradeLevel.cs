using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Academic;

public class GradeLevel : BaseEntity
{
    public int SchoolId { get; set; }
    public byte GradeNumber { get; set; }           // 1–12
    public string GradeName { get; set; } = string.Empty;
    public EducationLevel EducationLevel { get; set; }

    // Navigation
    public School School { get; set; } = null!;
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<Subject> Subjects { get; set; } = [];
}
