using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Attendance;

public class LeaveRequest : TenantEntity
{
    public int ParentId      { get; set; }
    public int StudentId     { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate   { get; set; }
    public string   Reason   { get; set; } = string.Empty;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public int?   ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public string?   ReviewNote    { get; set; }

    public User Parent     { get; set; } = null!;
    public User Student    { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
