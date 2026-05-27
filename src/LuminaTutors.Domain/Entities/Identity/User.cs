using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Entities.Profiles;

namespace LuminaTutors.Domain.Entities.Identity;

public class User : AuditableEntity
{
    public int SchoolId { get; set; }
    public int RoleId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }

    // Navigation — parent
    public School School { get; set; } = null!;
    public Role Role { get; set; } = null!;

    // Navigation — profiles (at most one of these will be populated per user)
    public StudentProfile? StudentProfile { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
    public ParentProfile? ParentProfile { get; set; }
    public SupervisorProfile? SupervisorProfile { get; set; }
    public AccountantProfile? AccountantProfile { get; set; }

    // Navigation — relationships
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ParentStudentRelation> ParentRelations { get; set; } = [];   // If Parent
    public ICollection<ParentStudentRelation> StudentRelations { get; set; } = [];  // If Student
    public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
}
