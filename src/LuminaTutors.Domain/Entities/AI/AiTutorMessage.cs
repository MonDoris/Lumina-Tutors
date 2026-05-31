using LuminaTutors.Domain.Common;

namespace LuminaTutors.Domain.Entities.AI;

public enum AiMessageRole { User, Assistant }

/// <summary>
/// Một tin nhắn trong phiên Gia Sư AI.
/// </summary>
public class AiTutorMessage : AuditableEntity
{
    public int           SessionId  { get; set; }
    public AiMessageRole Role       { get; set; }
    public string        Content    { get; set; } = string.Empty;
    public bool          IsFlagged  { get; set; } = false;

    // Navigation
    public AiTutorSession Session { get; set; } = null!;
}
