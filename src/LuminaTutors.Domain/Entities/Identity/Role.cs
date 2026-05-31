using LuminaTutors.Domain.Common;

namespace LuminaTutors.Domain.Entities.Identity;

public class Role : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = [];
}
