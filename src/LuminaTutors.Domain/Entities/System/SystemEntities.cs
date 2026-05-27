using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Domain.Entities.System;

// ─── SystemConfig ─────────────────────────────────────────────────────────────

public class SystemConfig
{
    public string ConfigKey { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string ConfigValue { get; set; } = string.Empty;
    public string DataType { get; set; } = "STRING";    // STRING | INT | BOOL | JSON
    public string? Description { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public School School { get; set; } = null!;
    public User? UpdatedBy { get; set; }
}

// ─── AuditLog (Immutable — no cascade delete) ────────────────────────────────

public class AuditLog
{
    public long LogId { get; set; }
    public int? SchoolId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }   // JSON snapshot before change
    public string? NewValues { get; set; }   // JSON snapshot after change
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
