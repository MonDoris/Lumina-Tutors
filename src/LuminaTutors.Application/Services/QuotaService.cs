using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Subscription;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

/// <summary>
/// Kiểm tra giới hạn tài khoản / lớp học của trường theo gói subscription.
/// Quota hiệu lực = quota gốc trong Plan + Σ ExtraQuota từ các RoleQuotaAddOn đang active.
/// Trường chưa có gói đang hoạt động → coi như quota = 0 (chặn tạo mới).
/// </summary>
public sealed class QuotaService : IQuotaService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<QuotaService> _logger;

    public QuotaService(IUnitOfWork uow, ILogger<QuotaService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Mã RoleCode lưu trong DB (chữ hoa) tương ứng enum.</summary>
    private static string DbCode(RoleCode role) => role switch
    {
        RoleCode.Admin      => "ADMIN",
        RoleCode.Teacher    => "TEACHER",
        RoleCode.Student    => "STUDENT",
        RoleCode.Parent     => "PARENT",
        RoleCode.Supervisor => "SUPERVISOR",
        RoleCode.Accountant => "ACCOUNTANT",
        _                   => role.ToString().ToUpperInvariant()
    };

    // ─── Status tổng hợp ──────────────────────────────────────────────────────

    public async Task<Result<QuotaStatusDto>> GetQuotaStatusAsync(int schoolId, CancellationToken ct = default)
    {
        var sub = await LoadActiveSubscriptionAsync(schoolId, ct);

        if (sub is null)
            return Result<QuotaStatusDto>.Success(new QuotaStatusDto { HasActiveSubscription = false });

        // Đếm tài khoản đang hoạt động theo từng role (1 query gom nhóm)
        var roleCounts = await _uow.Users.AsQueryable()
            .Where(u => u.SchoolId == schoolId && u.IsActive)
            .GroupBy(u => u.Role.RoleCode)
            .Select(g => new { RoleCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleCode, x => x.Count, ct);

        var classCount = await _uow.Classes.CountAsync(c => c.SchoolId == schoolId && c.IsActive, ct);

        int Used(RoleCode r) => roleCounts.GetValueOrDefault(DbCode(r), 0);

        QuotaSlotDto Slot(RoleCode r, int planMax) => new()
        {
            Used = Used(r),
            Max  = EffectiveMax(planMax, ExtraQuotaFor(sub, r))
        };

        var plan = sub.Plan;
        var status = new QuotaStatusDto
        {
            HasActiveSubscription = true,
            Teachers    = Slot(RoleCode.Teacher,    plan.MaxTeachers),
            Students    = Slot(RoleCode.Student,    plan.MaxStudents),
            Parents     = Slot(RoleCode.Parent,     plan.MaxParents),
            Admins      = Slot(RoleCode.Admin,      plan.MaxAdmins),
            Accountants = Slot(RoleCode.Accountant, plan.MaxAccountants),
            Supervisors = Slot(RoleCode.Supervisor, plan.MaxSupervisors),
            Classes     = new QuotaSlotDto
            {
                Used = classCount,
                Max  = EffectiveMax(plan.MaxClasses, ExtraClassesFor(sub))
            }
        };

        return Result<QuotaStatusDto>.Success(status);
    }

    // ─── Check tạo tài khoản ──────────────────────────────────────────────────

    public async Task<Result<bool>> CanAddUserAsync(int schoolId, RoleCode role, CancellationToken ct = default)
    {
        var sub = await LoadActiveSubscriptionAsync(schoolId, ct);
        if (sub is null)
            return Result<bool>.Failure(
                "Trường chưa có gói dịch vụ đang hoạt động. Vui lòng đăng ký gói trước khi tạo tài khoản.",
                "NO_ACTIVE_PLAN");

        var max = EffectiveMax(PlanMaxFor(sub.Plan, role), ExtraQuotaFor(sub, role));
        if (max == -1) return Result<bool>.Success(true);   // không giới hạn

        var code = DbCode(role);
        var used = await _uow.Users.CountAsync(
            u => u.SchoolId == schoolId && u.IsActive && u.Role.RoleCode == code, ct);

        if (used >= max)
            return Result<bool>.Failure(
                $"Đã quá số lượng {RoleLabel(role)} cho phép của gói ({used}/{max}). " +
                "Vui lòng nâng cấp gói hoặc mua thêm gói tài khoản (add-on).",
                "QUOTA_EXCEEDED");

        return Result<bool>.Success(true);
    }

    // ─── Check tạo lớp ────────────────────────────────────────────────────────

    public async Task<Result<bool>> CanAddClassAsync(int schoolId, CancellationToken ct = default)
    {
        var sub = await LoadActiveSubscriptionAsync(schoolId, ct);
        if (sub is null)
            return Result<bool>.Failure(
                "Trường chưa có gói dịch vụ đang hoạt động. Vui lòng đăng ký gói trước khi tạo lớp học.",
                "NO_ACTIVE_PLAN");

        var max = EffectiveMax(sub.Plan.MaxClasses, ExtraClassesFor(sub));
        if (max == -1) return Result<bool>.Success(true);

        var used = await _uow.Classes.CountAsync(c => c.SchoolId == schoolId && c.IsActive, ct);
        if (used >= max)
            return Result<bool>.Failure(
                $"Đã quá số lượng lớp học cho phép của gói ({used}/{max}). " +
                "Vui lòng nâng cấp gói hoặc mua thêm gói lớp học (add-on).",
                "QUOTA_EXCEEDED");

        return Result<bool>.Success(true);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SchoolSubscription?> LoadActiveSubscriptionAsync(int schoolId, CancellationToken ct)
    {
        var sub = await _uow.SchoolSubscriptions.FindOneAsync(
            s => s.SchoolId == schoolId,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.RoleQuotaAddOns).ThenInclude(a => a.AddOn),
            ct: ct);

        if (sub is null || sub.Status != SubscriptionStatus.Active || sub.CurrentPeriodEnd < Today)
            return null;
        return sub;
    }

    private static int PlanMaxFor(SubscriptionPlan plan, RoleCode role) => role switch
    {
        RoleCode.Teacher    => plan.MaxTeachers,
        RoleCode.Student    => plan.MaxStudents,
        RoleCode.Parent     => plan.MaxParents,
        RoleCode.Admin      => plan.MaxAdmins,
        RoleCode.Accountant => plan.MaxAccountants,
        RoleCode.Supervisor => plan.MaxSupervisors,
        _                   => -1
    };

    private static int ExtraQuotaFor(SchoolSubscription sub, RoleCode role) =>
        sub.RoleQuotaAddOns
            .Where(a => a.IsActive && a.ActiveUntil >= Today
                        && a.AddOn.TargetRole == role)
            .Sum(a => a.AddOn.ExtraQuota);

    private static int ExtraClassesFor(SchoolSubscription sub) =>
        sub.RoleQuotaAddOns
            .Where(a => a.IsActive && a.ActiveUntil >= Today)
            .Sum(a => a.AddOn.ExtraClasses);

    /// <summary>Gộp quota gốc + quota mua thêm. Quota gốc -1 (unlimited) thì giữ unlimited.</summary>
    private static int EffectiveMax(int planMax, int extra) => planMax == -1 ? -1 : planMax + extra;

    private static string RoleLabel(RoleCode role) => role switch
    {
        RoleCode.Teacher    => "giáo viên",
        RoleCode.Student    => "học sinh",
        RoleCode.Parent     => "phụ huynh",
        RoleCode.Admin      => "quản trị nhà trường",
        RoleCode.Accountant => "kế toán",
        RoleCode.Supervisor => "giám thị",
        _                   => role.ToString()
    };
}
