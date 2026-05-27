namespace LuminaTutors.Domain.Common;

/// <summary>
/// Base entity with primary key only — for immutable / lookup tables.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>
/// Auditable entity: tracks create/update timestamps.
/// Inherited by all business entities that change over time.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Multi-tenant auditable entity — all major business tables inherit this.
/// SchoolId is the tenant discriminator.
/// </summary>
public abstract class TenantEntity : AuditableEntity
{
    public int SchoolId { get; set; }
}
