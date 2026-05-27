using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Academic;

public class ClassEnrollment : AuditableEntity
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public DateOnly EnrolledDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public string? TransferNote { get; set; }

    // Navigation
    public Class Class { get; set; } = null!;
    public User Student { get; set; } = null!;
}
