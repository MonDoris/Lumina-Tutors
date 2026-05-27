using LuminaTutors.Domain.Common;

namespace LuminaTutors.Domain.Entities.Identity;

public class InviteLink : BaseEntity
{
    public int SchoolId { get; set; }
    public Guid Token { get; set; } = Guid.NewGuid();
    public int TargetRoleId { get; set; }
    public string? TargetEmail { get; set; }
    public int CreatedByUserId { get; set; }
    public int? LinkedStudentId { get; set; }    // Pre-links Parent invite to a Student
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UsedByUserId { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsValid => !IsRevoked && !IsExpired && !IsUsed;

    // Navigation
    public School School { get; set; } = null!;
    public Role TargetRole { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? LinkedStudent { get; set; }
    public User? UsedBy { get; set; }
}
