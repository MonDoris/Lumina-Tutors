using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Finance;
using LuminaTutors.Domain.Entities.System;

namespace LuminaTutors.Domain.Entities.Identity;

public class School : AuditableEntity
{
    public string SchoolCode { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Province { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LicenseCode { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<User> Users { get; set; } = [];
    public ICollection<AcademicYear> AcademicYears { get; set; } = [];
    public ICollection<GradeLevel> GradeLevels { get; set; } = [];
    public ICollection<Subject> Subjects { get; set; } = [];
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<SystemConfig> SystemConfigs { get; set; } = [];
}
