using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Kiểm tra và truy vấn giới hạn tài khoản / lớp học của trường theo gói subscription.
/// Quota hiệu lực = quota gốc trong Plan + tổng ExtraQuota từ các RoleQuotaAddOn đang active.
/// </summary>
public interface IQuotaService
{
    /// <summary>Trả về quota hiệu lực và số đang dùng cho từng role + lớp học của trường.</summary>
    Task<Result<QuotaStatusDto>> GetQuotaStatusAsync(int schoolId, CancellationToken ct = default);

    /// <summary>Kiểm tra trường còn slot để tạo thêm tài khoản với role chỉ định.</summary>
    Task<Result<bool>> CanAddUserAsync(int schoolId, RoleCode role, CancellationToken ct = default);

    /// <summary>Kiểm tra trường còn slot để tạo thêm lớp học.</summary>
    Task<Result<bool>> CanAddClassAsync(int schoolId, CancellationToken ct = default);
}

// ─── DTOs quota ──────────────────────────────────────────────────────────────

public class QuotaStatusDto
{
    public bool HasActiveSubscription { get; init; }

    public QuotaSlotDto Teachers    { get; init; } = new();
    public QuotaSlotDto Students    { get; init; } = new();
    public QuotaSlotDto Parents     { get; init; } = new();
    public QuotaSlotDto Admins      { get; init; } = new();
    public QuotaSlotDto Accountants { get; init; } = new();
    public QuotaSlotDto Supervisors { get; init; } = new();
    public QuotaSlotDto Classes     { get; init; } = new();
}

public class QuotaSlotDto
{
    public int  Used      { get; init; }
    public int  Max       { get; init; }           // -1 = không giới hạn
    public bool Unlimited => Max == -1;
    public bool IsNearLimit => !Unlimited && Max > 0 && (double)Used / Max >= 0.8;
    public bool IsFull      => !Unlimited && Used >= Max;
    public int  Remaining   => Unlimited ? int.MaxValue : Math.Max(0, Max - Used);
}
