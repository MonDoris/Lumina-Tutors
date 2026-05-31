using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Domain.Entities.AI;

/// <summary>
/// Một phiên hỏi đáp giữa học sinh và Gia Sư AI.
/// </summary>
public class AiTutorSession : TenantEntity
{
    public int    StudentId  { get; set; }
    public string Title      { get; set; } = string.Empty;
    public bool   IsReviewed { get; set; } = false;
    public bool   IsFlagged  { get; set; } = false;
    public string? AdminNote { get; set; }

    // Navigation
    public User   Student  { get; set; } = null!;
    public ICollection<AiTutorMessage> Messages { get; set; } = [];
}
