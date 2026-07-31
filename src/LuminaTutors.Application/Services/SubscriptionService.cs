using LuminaTutors.Application.DTOs.Subscription;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Subscription;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

/// <summary>
/// Nghiệp vụ gói dịch vụ cấp trường: nâng cấp Basic→Premium, mua add-on
/// (AI Tutor / Virtual Lab), đăng ký định kỳ tự gia hạn, xác nhận thanh toán
/// và kiểm tra entitlement cho phần gating ở tầng Web.
/// </summary>
public sealed class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IBillingEmailService _billingMail;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IUnitOfWork uow,
        IPasswordHasher<User> hasher,
        IBillingEmailService billingMail,
        ILogger<SubscriptionService> logger)
    {
        _uow         = uow;
        _hasher      = hasher;
        _billingMail = billingMail;
        _logger      = logger;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly AddCycle(DateOnly from, SubscriptionCycle cycle) => cycle switch
    {
        SubscriptionCycle.Monthly   => from.AddMonths(1),
        SubscriptionCycle.Quarterly => from.AddMonths(3),
        SubscriptionCycle.Yearly    => from.AddYears(1),
        _                           => from.AddMonths(1)
    };

    // ─── Overview ─────────────────────────────────────────────────────────────

    public async Task<Result<SubscriptionOverviewDto>> GetOverviewAsync(int schoolId, CancellationToken ct = default)
    {
        var sub   = await LoadSubscriptionAsync(schoolId, ct);
        var plans = (await _uow.SubscriptionPlans.FindAsync(p => p.IsActive, ct))
                    .OrderBy(p => p.Tier).ToList();
        var addOns = (await _uow.SubscriptionAddOns.FindAsync(a => a.IsActive, ct))
                    .OrderBy(a => a.Name).ToList();
        var quotaAddOns = (await _uow.RoleQuotaAddOns.FindAsync(a => a.IsActive, ct))
                    .OrderBy(a => a.TargetRole).ThenBy(a => a.Name).ToList();

        var current = MapCurrent(sub);

        var orders = (await _uow.SubscriptionOrders.FindAsync(
                o => o.SchoolId == schoolId,
                include: q => q.Include(o => o.Plan).Include(o => o.Items),
                ct: ct))
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => MapOrder(o))
            .ToList();

        var currentTier   = current.IsActive ? current.Tier : 0;
        var planDtos = plans.Select(p => new SubscriptionPlanDto
        {
            PlanId             = p.Id,
            PlanCode           = p.PlanCode,
            Name               = p.Name,
            Description        = p.Description,
            Tier               = p.Tier,
            MonthlyPrice       = p.MonthlyPrice,
            QuarterlyPrice     = p.QuarterlyPrice,
            YearlyPrice        = p.YearlyPrice,
            IncludesAiTutor    = p.IncludesAiTutor,
            IncludesVirtualLab = p.IncludesVirtualLab,
            IsCurrent          = current.IsActive && sub?.PlanId == p.Id,
            IsUpgrade          = current.IsActive && p.Tier > currentTier,
            MaxTeachers        = p.MaxTeachers,
            MaxStudents        = p.MaxStudents,
            MaxParents         = p.MaxParents,
            MaxAdmins          = p.MaxAdmins,
            MaxAccountants     = p.MaxAccountants,
            MaxSupervisors     = p.MaxSupervisors,
            MaxClasses         = p.MaxClasses
        }).ToList();

        var addOnDtos = addOns.Select(a =>
        {
            var feature       = a.Feature;
            var inPlan        = current.IsActive && sub!.Plan.Includes(feature);
            var ownedAddOn    = sub?.AddOns.FirstOrDefault(x => x.IsActive && x.AddOn.Feature == feature && x.ActiveUntil >= Today);
            return new SubscriptionAddOnDto
            {
                AddOnId        = a.Id,
                AddOnCode      = a.AddOnCode,
                Name           = a.Name,
                Description    = a.Description,
                Feature        = feature.ToString(),
                MonthlyPrice   = a.MonthlyPrice,
                QuarterlyPrice = a.QuarterlyPrice,
                YearlyPrice    = a.YearlyPrice,
                InCurrentPlan  = inPlan,
                IsOwned        = inPlan || ownedAddOn is not null,
                ActiveUntil    = ownedAddOn?.ActiveUntil
            };
        }).ToList();

        var quotaAddOnDtos = quotaAddOns.Select(a =>
        {
            var owned = sub?.RoleQuotaAddOns.FirstOrDefault(x => x.IsActive && x.AddOnId == a.Id && x.ActiveUntil >= Today);
            return new RoleQuotaAddOnDto
            {
                AddOnId        = a.Id,
                AddOnCode      = a.AddOnCode,
                Name           = a.Name,
                Description    = a.Description,
                TargetRole     = a.TargetRole,
                ExtraQuota     = a.ExtraQuota,
                ExtraClasses   = a.ExtraClasses,
                MonthlyPrice   = a.MonthlyPrice,
                QuarterlyPrice = a.QuarterlyPrice,
                YearlyPrice    = a.YearlyPrice,
                IsActive       = a.IsActive,
                IsOwned        = owned is not null,
                ActiveUntil    = owned?.ActiveUntil
            };
        }).ToList();

        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);

        return Result<SubscriptionOverviewDto>.Success(new SubscriptionOverviewDto
        {
            Current      = current,
            Plans        = planDtos,
            AddOns       = addOnDtos,
            QuotaAddOns  = quotaAddOnDtos,
            RecentOrders = orders,
            BillingEmail = school?.Email
        });
    }

    // ─── Email nhận hóa đơn ───────────────────────────────────────────────────

    public async Task<Result> UpdateBillingEmailAsync(int schoolId, string? email, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result.Failure("Không tìm thấy trường.", "SCHOOL_NOT_FOUND");

        var value = email?.Trim();
        if (!string.IsNullOrEmpty(value))
        {
            if (value.Length > 150)
                return Result.Failure("Email quá dài (tối đa 150 ký tự).", "EMAIL_TOO_LONG");
            var at = value.IndexOf('@');
            if (at <= 0 || at == value.Length - 1 || value.Contains(' ') || !value[(at + 1)..].Contains('.'))
                return Result.Failure("Email không hợp lệ.", "EMAIL_INVALID");
        }

        school.Email = string.IsNullOrEmpty(value) ? null : value;
        _uow.Schools.Update(school);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Trường {SchoolId} đổi email nhận hóa đơn thành {Email}", schoolId, school.Email ?? "(trống)");
        return Result.Success();
    }

    // ─── Subscribe / Upgrade ──────────────────────────────────────────────────

    public async Task<Result<SubscriptionOrderDto>> SubscribeOrUpgradeAsync(
        int schoolId, int userId, ChangePlanRequest request, CancellationToken ct = default)
    {
        var plan = await _uow.SubscriptionPlans.GetByIdAsync(request.PlanId, ct);
        if (plan is null || !plan.IsActive)
            return Result<SubscriptionOrderDto>.Failure("Gói dịch vụ không tồn tại.", "PLAN_NOT_FOUND");

        var sub = await LoadSubscriptionAsync(schoolId, ct);
        var isActiveNow = sub is not null && sub.Status == SubscriptionStatus.Active && sub.CurrentPeriodEnd >= Today;

        SubscriptionOrderType orderType;
        if (isActiveNow)
        {
            if (sub!.PlanId == plan.Id)
                return Result<SubscriptionOrderDto>.Failure(
                    "Trường đang dùng gói này. Dùng chức năng Gia hạn để kéo dài thời hạn.", "SAME_PLAN");
            if (plan.Tier <= sub.Plan.Tier)
                return Result<SubscriptionOrderDto>.Failure(
                    "Chỉ có thể nâng cấp lên gói cao hơn. Hạ gói sẽ áp dụng ở kỳ kế tiếp.", "NOT_UPGRADE");
            orderType = SubscriptionOrderType.Upgrade;
        }
        else
        {
            orderType = SubscriptionOrderType.New;
        }

        SubscriptionOrderDto dto = null!;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            // Đảm bảo có 1 bản ghi đăng ký để gắn đơn
            if (sub is null)
            {
                sub = new SchoolSubscription
                {
                    SchoolId         = schoolId,
                    PlanId           = plan.Id,
                    BillingCycle     = request.Cycle,
                    Status           = SubscriptionStatus.PendingPayment,
                    StartDate        = Today,
                    CurrentPeriodEnd = Today,      // chưa hiệu lực cho tới khi thanh toán
                    AutoRenew        = request.AutoRenew
                };
                await _uow.SchoolSubscriptions.AddAsync(sub, ct);
                await _uow.SaveChangesAsync(ct);
            }

            var periodStart = Today;
            var periodEnd   = AddCycle(periodStart, request.Cycle);
            var amount      = plan.PriceFor(request.Cycle);

            var order = await BuildOrderAsync(schoolId, userId, sub.Id, orderType, plan.Id,
                request.Cycle, amount, periodStart, periodEnd, ct);
            order.Items.Add(new SubscriptionOrderItem
            {
                ItemType    = SubscriptionItemType.Plan,
                RefId       = plan.Id,
                Description = $"Gói {plan.Name} ({CycleLabel(request.Cycle)})",
                Amount      = amount
            });
            order.Notes = request.AutoRenew ? "Đăng ký định kỳ (tự gia hạn)" : "Mua một lần (không tự gia hạn)";

            await _uow.SubscriptionOrders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            // Ghi nhận lựa chọn cycle/auto-renew để áp dụng khi thanh toán thành công
            sub.BillingCycle = request.Cycle;
            sub.AutoRenew    = request.AutoRenew;
            await _uow.SaveChangesAsync(ct);

            dto = MapOrder(order, plan);
        }, ct);

        _logger.LogInformation("Subscription order {Code} ({Type}) created for school {School}",
            dto.OrderCode, orderType, schoolId);
        return Result<SubscriptionOrderDto>.Success(dto);
    }

    // ─── Buy add-on ───────────────────────────────────────────────────────────

    public async Task<Result<SubscriptionOrderDto>> BuyAddOnAsync(
        int schoolId, int userId, int addOnId, CancellationToken ct = default)
    {
        var addOn = await _uow.SubscriptionAddOns.GetByIdAsync(addOnId, ct);
        if (addOn is null || !addOn.IsActive)
            return Result<SubscriptionOrderDto>.Failure("Tính năng không tồn tại.", "ADDON_NOT_FOUND");

        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null || sub.Status != SubscriptionStatus.Active || sub.CurrentPeriodEnd < Today)
            return Result<SubscriptionOrderDto>.Failure(
                "Cần có gói đang hoạt động trước khi mua thêm tính năng.", "NO_ACTIVE_PLAN");

        if (sub.Plan.Includes(addOn.Feature))
            return Result<SubscriptionOrderDto>.Failure(
                "Gói hiện tại đã bao gồm tính năng này.", "ALREADY_IN_PLAN");

        if (sub.AddOns.Any(a => a.IsActive && a.AddOnId == addOn.Id && a.ActiveUntil >= Today))
            return Result<SubscriptionOrderDto>.Failure("Trường đã sở hữu tính năng này.", "ALREADY_OWNED");

        var periodStart = Today;
        var periodEnd   = sub.CurrentPeriodEnd;                 // canh hết hạn theo kỳ của gói
        var amount      = addOn.PriceFor(sub.BillingCycle);

        SubscriptionOrderDto dto = null!;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var order = await BuildOrderAsync(schoolId, userId, sub.Id, SubscriptionOrderType.AddOn, null,
                sub.BillingCycle, amount, periodStart, periodEnd, ct);
            order.Items.Add(new SubscriptionOrderItem
            {
                ItemType    = SubscriptionItemType.AddOn,
                RefId       = addOn.Id,
                Description = $"Tính năng {addOn.Name}",
                Amount      = amount
            });

            await _uow.SubscriptionOrders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);
            dto = MapOrder(order, null);
        }, ct);

        _logger.LogInformation("Add-on order {Code} ({AddOn}) created for school {School}",
            dto.OrderCode, addOn.AddOnCode, schoolId);
        return Result<SubscriptionOrderDto>.Success(dto);
    }

    // ─── Buy quota add-on (slot tài khoản / lớp học) ──────────────────────────

    public async Task<Result<SubscriptionOrderDto>> BuyQuotaAddOnAsync(
        int schoolId, int userId, int quotaAddOnId, CancellationToken ct = default)
    {
        var addOn = await _uow.RoleQuotaAddOns.GetByIdAsync(quotaAddOnId, ct);
        if (addOn is null || !addOn.IsActive)
            return Result<SubscriptionOrderDto>.Failure("Gói mở rộng không tồn tại.", "QUOTA_ADDON_NOT_FOUND");

        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null || sub.Status != SubscriptionStatus.Active || sub.CurrentPeriodEnd < Today)
            return Result<SubscriptionOrderDto>.Failure(
                "Cần có gói đang hoạt động trước khi mua thêm slot.", "NO_ACTIVE_PLAN");

        if (sub.RoleQuotaAddOns.Any(a => a.IsActive && a.AddOnId == addOn.Id && a.ActiveUntil >= Today))
            return Result<SubscriptionOrderDto>.Failure("Trường đã sở hữu gói mở rộng này.", "ALREADY_OWNED");

        var periodStart = Today;
        var periodEnd   = sub.CurrentPeriodEnd;                 // canh hết hạn theo kỳ của gói
        var amount      = addOn.PriceFor(sub.BillingCycle);

        SubscriptionOrderDto dto = null!;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var order = await BuildOrderAsync(schoolId, userId, sub.Id, SubscriptionOrderType.AddOn, null,
                sub.BillingCycle, amount, periodStart, periodEnd, ct);
            order.Items.Add(new SubscriptionOrderItem
            {
                ItemType    = SubscriptionItemType.QuotaAddOn,
                RefId       = addOn.Id,
                Description = $"Gói mở rộng {addOn.Name}",
                Amount      = amount
            });

            await _uow.SubscriptionOrders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);
            dto = MapOrder(order, null);
        }, ct);

        _logger.LogInformation("Quota add-on order {Code} ({AddOn}) created for school {School}",
            dto.OrderCode, addOn.AddOnCode, schoolId);
        return Result<SubscriptionOrderDto>.Success(dto);
    }

    // ─── Renewal (manual) ─────────────────────────────────────────────────────

    public async Task<Result<SubscriptionOrderDto>> CreateRenewalOrderAsync(
        int schoolId, int userId, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null || sub.Status == SubscriptionStatus.Cancelled)
            return Result<SubscriptionOrderDto>.Failure("Chưa có đăng ký để gia hạn.", "NO_SUBSCRIPTION");

        var existingPending = sub.Orders.FirstOrDefault(
            o => o.OrderType == SubscriptionOrderType.Renewal && o.Status == SubscriptionOrderStatus.Pending);
        if (existingPending is not null)
            return Result<SubscriptionOrderDto>.Success(MapOrder(existingPending, sub.Plan));

        SubscriptionOrderDto dto = null!;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var order = await BuildRenewalOrderAsync(sub, userId, ct);
            await _uow.SaveChangesAsync(ct);   // lưu trước để order có Id
            dto = MapOrder(order, sub.Plan);
        }, ct);

        return Result<SubscriptionOrderDto>.Success(dto);
    }

    // ─── Auto-renew toggle / cancel ───────────────────────────────────────────

    public async Task<Result> SetAutoRenewAsync(int schoolId, bool enabled, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null)
            return Result.Failure("Chưa có đăng ký.", "NO_SUBSCRIPTION");

        sub.AutoRenew = enabled;
        if (enabled && sub.Status == SubscriptionStatus.Cancelled && sub.CurrentPeriodEnd >= Today)
            sub.Status = SubscriptionStatus.Active;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(int schoolId, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null)
            return Result.Failure("Chưa có đăng ký.", "NO_SUBSCRIPTION");

        sub.AutoRenew   = false;
        sub.CancelledAt = DateTime.UtcNow;
        // Vẫn dùng đến hết kỳ đã trả; chỉ chuyển Cancelled nếu đã hết hạn.
        if (sub.CurrentPeriodEnd < Today)
            sub.Status = SubscriptionStatus.Cancelled;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Subscription cancelled (auto-renew off) for school {School}", schoolId);
        return Result.Success();
    }

    // ─── Get order ────────────────────────────────────────────────────────────

    public async Task<Result<SubscriptionOrderDto>> GetOrderAsync(int orderId, int schoolId, CancellationToken ct = default)
    {
        var order = await _uow.SubscriptionOrders.FindOneAsync(
            o => o.Id == orderId && o.SchoolId == schoolId,
            include: q => q.Include(o => o.Plan).Include(o => o.Items),
            ct: ct);
        if (order is null)
            return Result<SubscriptionOrderDto>.Failure("Đơn không tồn tại.", "NOT_FOUND");
        return Result<SubscriptionOrderDto>.Success(MapOrder(order));
    }

    // ─── Đổi chu kỳ đơn ngay tại trang thanh toán ─────────────────────────────

    public async Task<Result> ChangeOrderCycleAsync(
        int orderId, int schoolId, SubscriptionCycle cycle, CancellationToken ct = default)
    {
        var order = await _uow.SubscriptionOrders.FindOneAsync(
            o => o.Id == orderId && o.SchoolId == schoolId,
            include: q => q.Include(o => o.Plan).Include(o => o.Items).Include(o => o.Subscription),
            ct: ct);

        if (order is null)                                return Result.Failure("Đơn không tồn tại.", "NOT_FOUND");
        if (order.Status == SubscriptionOrderStatus.Paid) return Result.Failure("Đơn đã thanh toán, không đổi được chu kỳ.", "PAID");
        if (order.PlanId is null || order.Plan is null)   return Result.Failure("Chỉ đổi chu kỳ cho đơn mua gói.", "NOT_PLAN");

        var amount = order.Plan.PriceFor(cycle);
        order.BillingCycle = cycle;
        order.Amount       = amount;
        order.PeriodEnd    = AddCycle(order.PeriodStart, cycle);

        var planItem = order.Items.FirstOrDefault(i => i.ItemType == SubscriptionItemType.Plan);
        if (planItem is not null)
        {
            planItem.Amount      = amount;
            planItem.Description = $"Gói {order.Plan.Name} ({CycleLabel(cycle)})";
        }

        if (order.Subscription is not null)
            order.Subscription.BillingCycle = cycle;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── Confirm payment (idempotent) ─────────────────────────────────────────

    public async Task<SubscriptionConfirmResult> ConfirmOrderPaymentAsync(
        int orderId, decimal paidAmount, PaymentMethod method,
        string? transactionCode, string? gatewayRaw, CancellationToken ct = default)
    {
        var order = await _uow.SubscriptionOrders.FindOneAsync(
            o => o.Id == orderId,
            include: q => q.Include(o => o.Items)
                           .Include(o => o.Subscription).ThenInclude(s => s.Plan)
                           .Include(o => o.Subscription).ThenInclude(s => s.AddOns)
                           .Include(o => o.Subscription).ThenInclude(s => s.RoleQuotaAddOns),
            ct: ct);

        if (order is null)                                          return SubscriptionConfirmResult.NotFound;
        if (order.Status == SubscriptionOrderStatus.Paid)          return SubscriptionConfirmResult.AlreadyConfirmed;
        if (paidAmount != order.Amount)                            return SubscriptionConfirmResult.InvalidAmount;

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                order.Status          = SubscriptionOrderStatus.Paid;
                order.PaidAt          = DateTime.UtcNow;
                order.PaymentMethod   = method;
                order.TransactionCode = transactionCode;
                order.GatewayResponse = gatewayRaw;

                var sub = order.Subscription;

                switch (order.OrderType)
                {
                    case SubscriptionOrderType.New:
                    case SubscriptionOrderType.Renewal:
                    case SubscriptionOrderType.Upgrade:
                        if (order.PlanId.HasValue) sub.PlanId = order.PlanId.Value;
                        sub.BillingCycle     = order.BillingCycle;
                        sub.StartDate        = sub.Status == SubscriptionStatus.Active ? sub.StartDate : order.PeriodStart;
                        sub.CurrentPeriodEnd = order.PeriodEnd;
                        sub.Status           = SubscriptionStatus.Active;
                        // Gia hạn: kéo dài các add-on đang hoạt động theo kỳ mới
                        if (order.OrderType == SubscriptionOrderType.Renewal)
                        {
                            foreach (var a in sub.AddOns.Where(a => a.IsActive))
                                a.ActiveUntil = order.PeriodEnd;
                            foreach (var a in sub.RoleQuotaAddOns.Where(a => a.IsActive))
                                a.ActiveUntil = order.PeriodEnd;
                        }
                        break;

                    case SubscriptionOrderType.AddOn:
                        foreach (var item in order.Items.Where(i => i.ItemType == SubscriptionItemType.AddOn))
                        {
                            var existing = sub.AddOns.FirstOrDefault(a => a.AddOnId == item.RefId);
                            if (existing is not null)
                            {
                                existing.IsActive    = true;
                                existing.ActiveUntil = order.PeriodEnd;
                            }
                            else
                            {
                                await _uow.SchoolSubscriptionAddOns.AddAsync(new SchoolSubscriptionAddOn
                                {
                                    SubscriptionId = sub.Id,
                                    AddOnId        = item.RefId,
                                    IsActive       = true,
                                    ActiveUntil    = order.PeriodEnd
                                }, ct);
                            }
                        }
                        // Kích hoạt các gói mở rộng quota mua trong cùng đơn
                        foreach (var item in order.Items.Where(i => i.ItemType == SubscriptionItemType.QuotaAddOn))
                        {
                            var existing = sub.RoleQuotaAddOns.FirstOrDefault(a => a.AddOnId == item.RefId);
                            if (existing is not null)
                            {
                                existing.IsActive    = true;
                                existing.ActiveUntil = order.PeriodEnd;
                            }
                            else
                            {
                                await _uow.SchoolRoleQuotaAddOns.AddAsync(new SchoolRoleQuotaAddOn
                                {
                                    SubscriptionId = sub.Id,
                                    AddOnId        = item.RefId,
                                    IsActive       = true,
                                    ActiveUntil    = order.PeriodEnd
                                }, ct);
                            }
                        }
                        break;
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Subscription order {Id} confirmed paid ({Method}) for school {School}",
                orderId, method, order.SchoolId);

            // Gửi hóa đơn về nhà trường. Chỉ chạy đúng 1 lần vì đơn đã chuyển sang Paid
            // (lần xác nhận sau trả AlreadyConfirmed và thoát sớm ở trên).
            // Lỗi gửi mail KHÔNG được làm hỏng giao dịch đã thanh toán.
            try
            {
                var mail = await _billingMail.SendSubscriptionReceiptAsync(orderId, ct);
                if (!mail.IsSuccess)
                    _logger.LogWarning("Không gửi được hóa đơn đơn {Id}: {Error}", orderId, mail.Error);
            }
            catch (Exception mailEx)
            {
                _logger.LogError(mailEx, "Lỗi khi gửi hóa đơn đơn {Id}", orderId);
            }

            return SubscriptionConfirmResult.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Confirm subscription order {Id} failed", orderId);
            return SubscriptionConfirmResult.Error;
        }
    }

    // ─── Entitlement check (dùng cho gating) ──────────────────────────────────

    public async Task<bool> HasFeatureAsync(int schoolId, PremiumFeature feature, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(schoolId, ct);
        if (sub is null || sub.Status != SubscriptionStatus.Active || sub.CurrentPeriodEnd < Today)
            return false;

        if (sub.Plan.Includes(feature)) return true;
        return sub.AddOns.Any(a => a.IsActive && a.ActiveUntil >= Today && a.AddOn.Feature == feature);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int schoolId, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(schoolId, ct);
        return sub is not null && sub.Status == SubscriptionStatus.Active && sub.CurrentPeriodEnd >= Today;
    }

    // ─── Chi tiết một trường (SYSADMIN) ───────────────────────────────────────

    public async Task<Result<SchoolDetailDto>> GetSchoolDetailAsync(int schoolId, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result<SchoolDetailDto>.Failure("Trường không tồn tại.", "NOT_FOUND");

        var users = await _uow.Users.FindAsync(
            u => u.SchoolId == schoolId,
            include: q => q.Include(u => u.Role), ct: ct);

        List<SchoolMemberDto> ByRole(string code) => users
            .Where(u => u.Role.RoleCode == code)
            .OrderBy(u => u.FullName)
            .Select(u => new SchoolMemberDto
            {
                UserId      = u.Id,
                FullName    = u.FullName,
                Email       = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive    = u.IsActive
            })
            .ToList();

        var teachers    = ByRole("TEACHER");
        var students    = ByRole("STUDENT");
        var parents     = ByRole("PARENT");
        var accountants = ByRole("ACCOUNTANT");

        // Xếp loại học lực: GPA mỗi HS = trung bình ĐTBm các môn đã tính tổng kết.
        var gradeBooks = await _uow.GradeBooks.FindAsync(
            gb => gb.SchoolId == schoolId && gb.IsCalculated && gb.AverageScore != null, ct);

        var order  = new[] { "Xuất sắc", "Giỏi", "Trung bình", "Yếu", "Kém" };
        var counts = order.ToDictionary(k => k, _ => 0);
        foreach (var g in gradeBooks.GroupBy(gb => gb.StudentId))
            counts[ClassifyHocLuc(g.Average(x => x.AverageScore!.Value))]++;

        var graded = counts.Values.Sum();
        var classification = order
            .Where(k => counts[k] > 0)
            .Select(k => new ClassificationSliceDto
            {
                Label   = k,
                Count   = counts[k],
                Percent = graded == 0 ? 0 : Math.Round(counts[k] * 100.0 / graded, 1)
            })
            .ToList();

        var sub = await LoadSubscriptionAsync(schoolId, ct);
        var subActive = sub is not null && sub.Status == SubscriptionStatus.Active && sub.CurrentPeriodEnd >= Today;

        return Result<SchoolDetailDto>.Success(new SchoolDetailDto
        {
            SchoolId           = school.Id,
            SchoolCode         = school.SchoolCode,
            SchoolName         = school.SchoolName,
            IsActive           = school.IsActive,
            PlanName           = sub?.Plan?.Name ?? "Chưa đăng ký",
            SubscriptionActive = subActive,
            TotalStudents      = students.Count,
            TotalTeachers      = teachers.Count,
            TotalParents       = parents.Count,
            TotalAccountants   = accountants.Count,
            Teachers           = teachers,
            Students           = students,
            Parents            = parents,
            Accountants        = accountants,
            Classification     = classification,
            GradedStudentCount = graded
        });
    }

    private static string ClassifyHocLuc(decimal gpa) => gpa switch
    {
        >= 9.0m => "Xuất sắc",
        >= 7.0m => "Giỏi",
        >= 5.0m => "Trung bình",
        >= 3.5m => "Yếu",
        _       => "Kém"
    };

    // ─── Process due renewals (cron) ──────────────────────────────────────────

    public async Task<int> ProcessDueRenewalsAsync(CancellationToken ct = default)
    {
        var dueSubs = await _uow.SchoolSubscriptions.FindAsync(
            s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd < Today,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.AddOns).ThenInclude(a => a.AddOn)
                           .Include(s => s.RoleQuotaAddOns).ThenInclude(a => a.AddOn)
                           .Include(s => s.Orders),
            ct: ct);

        if (dueSubs.Count == 0) return 0;

        var created = 0;
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            foreach (var sub in dueSubs)
            {
                sub.Status = SubscriptionStatus.Expired;

                if (sub.AutoRenew &&
                    !sub.Orders.Any(o => o.OrderType == SubscriptionOrderType.Renewal &&
                                         o.Status    == SubscriptionOrderStatus.Pending))
                {
                    await BuildRenewalOrderAsync(sub, sub.Orders.FirstOrDefault()?.CreatedByUserId ?? 0, ct);
                    created++;
                }
            }
            await _uow.SaveChangesAsync(ct);
        }, ct);

        if (created > 0)
            _logger.LogInformation("Auto-renew: created {Count} pending renewal order(s) for due subscriptions", created);
        return created;
    }

    // ─── E-Selling admin (SYSADMIN): catalog gói/add-on ───────────────────────

    public async Task<Result<CatalogDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        var plans  = (await _uow.SubscriptionPlans.GetAllAsync(ct)).OrderBy(p => p.Tier).ToList();
        var addOns = (await _uow.SubscriptionAddOns.GetAllAsync(ct)).OrderBy(a => a.Name).ToList();
        var quotaAddOns = (await _uow.RoleQuotaAddOns.GetAllAsync(ct))
                          .OrderBy(a => a.TargetRole).ThenBy(a => a.Name).ToList();

        return Result<CatalogDto>.Success(new CatalogDto
        {
            Plans = plans.Select(p => new SubscriptionPlanDto
            {
                PlanId             = p.Id,
                PlanCode           = p.PlanCode,
                Name               = p.Name,
                Description        = p.Description,
                Tier               = p.Tier,
                MonthlyPrice       = p.MonthlyPrice,
                QuarterlyPrice     = p.QuarterlyPrice,
                YearlyPrice        = p.YearlyPrice,
                IncludesAiTutor    = p.IncludesAiTutor,
                IncludesVirtualLab = p.IncludesVirtualLab,
                IsActive           = p.IsActive,
                MaxTeachers        = p.MaxTeachers,
                MaxStudents        = p.MaxStudents,
                MaxParents         = p.MaxParents,
                MaxAdmins          = p.MaxAdmins,
                MaxAccountants     = p.MaxAccountants,
                MaxSupervisors     = p.MaxSupervisors,
                MaxClasses         = p.MaxClasses
            }).ToList(),
            AddOns = addOns.Select(a => new SubscriptionAddOnDto
            {
                AddOnId        = a.Id,
                AddOnCode      = a.AddOnCode,
                Name           = a.Name,
                Description    = a.Description,
                Feature        = a.Feature.ToString(),
                MonthlyPrice   = a.MonthlyPrice,
                QuarterlyPrice = a.QuarterlyPrice,
                YearlyPrice    = a.YearlyPrice,
                IsActive       = a.IsActive
            }).ToList(),
            QuotaAddOns = quotaAddOns.Select(a => new RoleQuotaAddOnDto
            {
                AddOnId        = a.Id,
                AddOnCode      = a.AddOnCode,
                Name           = a.Name,
                Description    = a.Description,
                TargetRole     = a.TargetRole,
                ExtraQuota     = a.ExtraQuota,
                ExtraClasses   = a.ExtraClasses,
                MonthlyPrice   = a.MonthlyPrice,
                QuarterlyPrice = a.QuarterlyPrice,
                YearlyPrice    = a.YearlyPrice,
                IsActive       = a.IsActive
            }).ToList()
        });
    }

    public async Task<Result> SavePlanAsync(PlanEditRequest r, CancellationToken ct = default)
    {
        var code = r.PlanCode.Trim().ToUpperInvariant();
        if (r.PlanId is null or 0)
        {
            if (await _uow.SubscriptionPlans.AnyAsync(p => p.PlanCode == code, ct))
                return Result.Failure("Mã gói đã tồn tại.", "DUP_CODE");
            var plan = new SubscriptionPlan();
            ApplyPlan(plan, r, code);
            await _uow.SubscriptionPlans.AddAsync(plan, ct);
        }
        else
        {
            var plan = await _uow.SubscriptionPlans.GetByIdAsync(r.PlanId.Value, ct);
            if (plan is null) return Result.Failure("Gói không tồn tại.", "NOT_FOUND");
            if (await _uow.SubscriptionPlans.AnyAsync(p => p.PlanCode == code && p.Id != plan.Id, ct))
                return Result.Failure("Mã gói đã tồn tại.", "DUP_CODE");
            ApplyPlan(plan, r, code);
        }
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Catalog: saved plan {Code}", code);
        return Result.Success();
    }

    public async Task<Result> TogglePlanActiveAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _uow.SubscriptionPlans.GetByIdAsync(planId, ct);
        if (plan is null) return Result.Failure("Gói không tồn tại.", "NOT_FOUND");
        plan.IsActive = !plan.IsActive;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SaveAddOnAsync(AddOnEditRequest r, CancellationToken ct = default)
    {
        var code = r.AddOnCode.Trim().ToUpperInvariant();
        if (r.AddOnId is null or 0)
        {
            if (await _uow.SubscriptionAddOns.AnyAsync(a => a.AddOnCode == code, ct))
                return Result.Failure("Mã add-on đã tồn tại.", "DUP_CODE");
            var addOn = new SubscriptionAddOn();
            ApplyAddOn(addOn, r, code);
            await _uow.SubscriptionAddOns.AddAsync(addOn, ct);
        }
        else
        {
            var addOn = await _uow.SubscriptionAddOns.GetByIdAsync(r.AddOnId.Value, ct);
            if (addOn is null) return Result.Failure("Add-on không tồn tại.", "NOT_FOUND");
            if (await _uow.SubscriptionAddOns.AnyAsync(a => a.AddOnCode == code && a.Id != addOn.Id, ct))
                return Result.Failure("Mã add-on đã tồn tại.", "DUP_CODE");
            ApplyAddOn(addOn, r, code);
        }
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Catalog: saved add-on {Code}", code);
        return Result.Success();
    }

    public async Task<Result> ToggleAddOnActiveAsync(int addOnId, CancellationToken ct = default)
    {
        var addOn = await _uow.SubscriptionAddOns.GetByIdAsync(addOnId, ct);
        if (addOn is null) return Result.Failure("Add-on không tồn tại.", "NOT_FOUND");
        addOn.IsActive = !addOn.IsActive;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SaveQuotaAddOnAsync(QuotaAddOnEditRequest r, CancellationToken ct = default)
    {
        var code = r.AddOnCode.Trim().ToUpperInvariant();
        if (r.ExtraQuota <= 0 && r.ExtraClasses <= 0)
            return Result.Failure("Phải nhập số slot tài khoản hoặc số lớp mở rộng (> 0).", "EMPTY_QUOTA");

        if (r.AddOnId is null or 0)
        {
            if (await _uow.RoleQuotaAddOns.AnyAsync(a => a.AddOnCode == code, ct))
                return Result.Failure("Mã gói mở rộng đã tồn tại.", "DUP_CODE");
            var addOn = new RoleQuotaAddOn();
            ApplyQuotaAddOn(addOn, r, code);
            await _uow.RoleQuotaAddOns.AddAsync(addOn, ct);
        }
        else
        {
            var addOn = await _uow.RoleQuotaAddOns.GetByIdAsync(r.AddOnId.Value, ct);
            if (addOn is null) return Result.Failure("Gói mở rộng không tồn tại.", "NOT_FOUND");
            if (await _uow.RoleQuotaAddOns.AnyAsync(a => a.AddOnCode == code && a.Id != addOn.Id, ct))
                return Result.Failure("Mã gói mở rộng đã tồn tại.", "DUP_CODE");
            ApplyQuotaAddOn(addOn, r, code);
        }
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Catalog: saved quota add-on {Code}", code);
        return Result.Success();
    }

    public async Task<Result> ToggleQuotaAddOnActiveAsync(int addOnId, CancellationToken ct = default)
    {
        var addOn = await _uow.RoleQuotaAddOns.GetByIdAsync(addOnId, ct);
        if (addOn is null) return Result.Failure("Gói mở rộng không tồn tại.", "NOT_FOUND");
        addOn.IsActive = !addOn.IsActive;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SchoolSubscriptionRowDto>>> GetAllSubscriptionsAsync(CancellationToken ct = default)
    {
        var subs = await _uow.SchoolSubscriptions.FindAsync(
            s => true,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.AddOns).ThenInclude(a => a.AddOn),
            ct: ct);
        var subBySchool = subs
            .GroupBy(s => s.SchoolId)
            .ToDictionary(g => g.Key, g => g.First());

        var schools = await _uow.Schools.GetAllAsync(ct);

        // Lịch sử thay đổi gói (8 đơn gần nhất mỗi trường)
        var orders = await _uow.SubscriptionOrders.FindAsync(
            o => true, include: q => q.Include(o => o.Plan), ct: ct);
        var historyBySchool = orders
            .GroupBy(o => o.SchoolId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PlanHistoryItemDto>)g
                .OrderByDescending(o => o.CreatedAt).Take(8)
                .Select(o => new PlanHistoryItemDto(
                    o.CreatedAt, o.OrderType.ToString(),
                    o.Plan != null ? o.Plan.Name : "—",
                    o.BillingCycle.ToString(), o.Status.ToString(), o.Amount))
                .ToList());

        // Liệt kê MỌI trường — trường chưa mua gói hiển thị "Chưa đăng ký"
        var rows = schools.OrderBy(s => s.SchoolName).Select(school =>
        {
            if (subBySchool.TryGetValue(school.Id, out var s))
            {
                return new SchoolSubscriptionRowDto
                {
                    HasSubscription  = true,
                    SchoolId         = school.Id,
                    SchoolCode       = school.SchoolCode,
                    SchoolName       = school.SchoolName,
                    PlanName         = s.Plan.Name,
                    Status           = s.Status.ToString(),
                    BillingCycle     = s.BillingCycle.ToString(),
                    CurrentPeriodEnd = s.CurrentPeriodEnd,
                    AutoRenew        = s.AutoRenew,
                    IsActive         = s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd >= Today,
                    AddOns           = string.Join(", ", s.AddOns
                                           .Where(a => a.IsActive && a.ActiveUntil >= Today)
                                           .Select(a => a.AddOn.Name)),
                    CurrentPlanId    = s.PlanId,
                    History          = historyBySchool.GetValueOrDefault(school.Id, new List<PlanHistoryItemDto>())
                };
            }
            return new SchoolSubscriptionRowDto
            {
                HasSubscription = false,
                SchoolId        = school.Id,
                SchoolCode      = school.SchoolCode,
                SchoolName      = school.SchoolName,
                Status          = "NotSubscribed",
                IsActive        = false,
                AddOns          = "",
                History         = historyBySchool.GetValueOrDefault(school.Id, new List<PlanHistoryItemDto>())
            };
        }).ToList();

        return Result<IReadOnlyList<SchoolSubscriptionRowDto>>.Success(rows);
    }

    // ─── E-Selling admin: điều chỉnh gói trực tiếp cho một trường ──────────────

    public async Task<Result> AdminChangePlanAsync(
        int schoolId, int planId, SubscriptionCycle cycle, bool autoRenew, int byUserId, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result.Failure("Trường không tồn tại.", "NOT_FOUND");
        var plan = await _uow.SubscriptionPlans.GetByIdAsync(planId, ct);
        if (plan is null || !plan.IsActive) return Result.Failure("Gói không hợp lệ.", "NO_PLAN");

        var periodStart = Today;
        var periodEnd   = AddCycle(Today, cycle);

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var sub = await _uow.SchoolSubscriptions.FindOneAsync(s => s.SchoolId == schoolId, ct: ct);
                var isNew = sub is null;
                if (sub is null)
                {
                    sub = new SchoolSubscription { SchoolId = schoolId, StartDate = periodStart };
                    await _uow.SchoolSubscriptions.AddAsync(sub, ct);
                }
                sub.PlanId           = planId;
                sub.BillingCycle     = cycle;
                sub.AutoRenew        = autoRenew;
                sub.Status           = SubscriptionStatus.Active;
                sub.CurrentPeriodEnd = periodEnd;
                sub.CancelledAt      = null;
                await _uow.SaveChangesAsync(ct);   // để có sub.Id

                var order = await BuildOrderAsync(schoolId, byUserId, sub.Id,
                    isNew ? SubscriptionOrderType.New : SubscriptionOrderType.Upgrade,
                    planId, cycle, plan.PriceFor(cycle), periodStart, periodEnd, ct);
                order.Status          = SubscriptionOrderStatus.Paid;
                order.PaidAt          = DateTime.UtcNow;
                order.PaymentMethod   = PaymentMethod.BankTransfer;
                order.GatewayResponse = "Điều chỉnh trực tiếp bởi Quản trị hệ thống";
                await _uow.SubscriptionOrders.AddAsync(order, ct);
                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("SYSADMIN {By} điều chỉnh gói trường {School} → plan {Plan} ({Cycle})",
                byUserId, schoolId, planId, cycle);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminChangePlan failed for school {School}", schoolId);
            return Result.Failure("Có lỗi khi điều chỉnh gói.", "ERROR");
        }
    }

    public async Task<Result<IReadOnlyList<PaidOrderDto>>> GetPaidOrdersAsync(CancellationToken ct = default)
    {
        var schools = (await _uow.Schools.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.SchoolName);
        var orders  = await _uow.SubscriptionOrders.FindAsync(
            o => o.Status == SubscriptionOrderStatus.Paid,
            include: q => q.Include(o => o.Plan), ct: ct);

        var list = orders
            .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
            .Select(o => new PaidOrderDto(
                o.Id, o.OrderCode,
                schools.GetValueOrDefault(o.SchoolId, $"Trường #{o.SchoolId}"),
                o.Plan != null ? o.Plan.Name : "—",
                o.OrderType.ToString(), o.BillingCycle.ToString(),
                o.Amount, o.PeriodStart, o.PeriodEnd, o.PaidAt))
            .ToList();

        return Result<IReadOnlyList<PaidOrderDto>>.Success(list);
    }

    // ─── Doanh thu ────────────────────────────────────────────────────────────

    public async Task<Result<RevenueReportDto>> GetRevenueReportAsync(CancellationToken ct = default)
    {
        var paid = await _uow.SubscriptionOrders.FindAsync(
            o => o.Status == SubscriptionOrderStatus.Paid, ct);

        var now   = DateTime.UtcNow;
        var total = paid.Sum(o => o.Amount);
        var thisMonth = paid
            .Where(o => o.PaidAt.HasValue && o.PaidAt.Value.Year == now.Year && o.PaidAt.Value.Month == now.Month)
            .Sum(o => o.Amount);

        var monthly = new List<RevenuePointDto>();
        var firstOfMonth = new DateTime(now.Year, now.Month, 1);
        for (var i = 11; i >= 0; i--)
        {
            var d   = firstOfMonth.AddMonths(-i);
            var amt = paid
                .Where(o => o.PaidAt.HasValue && o.PaidAt.Value.Year == d.Year && o.PaidAt.Value.Month == d.Month)
                .Sum(o => o.Amount);
            monthly.Add(new RevenuePointDto { Label = d.ToString("MM/yy"), Amount = amt });
        }

        var activeSubs = await _uow.SchoolSubscriptions.CountAsync(
            s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd >= Today, ct);

        return Result<RevenueReportDto>.Success(new RevenueReportDto
        {
            TotalRevenue        = total,
            ThisMonthRevenue    = thisMonth,
            PaidOrders          = paid.Count,
            ActiveSubscriptions = activeSubs,
            Monthly             = monthly
        });
    }

    // ─── Onboard trường mới + tài khoản Nhà trường ────────────────────────────

    public async Task<Result> OnboardSchoolAsync(OnboardSchoolRequest r, CancellationToken ct = default)
    {
        var email = r.AdminEmail.Trim().ToLowerInvariant();   // admin giữ domain gốc — đây là domain gốc của trường

        var role = (await _uow.Roles.FindAsync(x => x.RoleCode == "ADMIN", ct)).FirstOrDefault();
        if (role is null)
            return Result.Failure("Thiếu vai trò Nhà trường (ADMIN) trong hệ thống.", "CONFIG");

        var code = string.IsNullOrWhiteSpace(r.SchoolCode)
            ? GenerateSchoolCode(r.SchoolName)
            : r.SchoolCode.Trim().ToUpperInvariant();
        if (await _uow.Schools.AnyAsync(s => s.SchoolCode == code, ct))
            code = $"{code}{Random.Shared.Next(100, 999)}";

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var school = new School
                {
                    SchoolCode = code,
                    SchoolName = r.SchoolName.Trim(),
                    IsActive   = true
                };
                await _uow.Schools.AddAsync(school, ct);
                await _uow.SaveChangesAsync(ct);   // để có school.Id

                var user = new User
                {
                    SchoolId        = school.Id,
                    RoleId          = role.Id,
                    Email           = email,
                    FullName        = r.AdminFullName.Trim(),
                    IsActive        = true,
                    IsEmailVerified = true,
                    PasswordHash    = string.Empty
                };
                user.PasswordHash = _hasher.HashPassword(user, r.AdminPassword);
                await _uow.Users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Onboarded school {Name} ({Code}) with Nhà trường admin {Email}",
                r.SchoolName, code, email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboard school failed for {Name}", r.SchoolName);
            return Result.Failure("Có lỗi khi tạo trường. Vui lòng thử lại.", "ERROR");
        }
    }

    private static string GenerateSchoolCode(string name)
    {
        var ascii = new string(name.Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                                    .Take(8).ToArray()).ToUpperInvariant();
        return string.IsNullOrEmpty(ascii) ? $"SCH{Random.Shared.Next(1000, 9999)}" : ascii;
    }

    // ─── Quản lý tài khoản trường (CRUD) ──────────────────────────────────────

    public async Task<Result<IReadOnlyList<SchoolAccountDto>>> GetSchoolsAsync(CancellationToken ct = default)
    {
        var schools = await _uow.Schools.GetAllAsync(ct);
        var admins = (await _uow.Users.FindAsync(u => u.Role.RoleCode == "ADMIN", ct))
            .GroupBy(u => u.SchoolId).ToDictionary(g => g.Key, g => g.OrderBy(u => u.Id).First());
        var subs = (await _uow.SchoolSubscriptions.FindAsync(s => true,
                        include: q => q.Include(s => s.Plan), ct))
            .GroupBy(s => s.SchoolId).ToDictionary(g => g.Key, g => g.First());

        var rows = new List<SchoolAccountDto>();
        foreach (var sc in schools.OrderBy(s => s.SchoolName))
        {
            admins.TryGetValue(sc.Id, out var ad);
            subs.TryGetValue(sc.Id, out var sub);
            var students = await _uow.StudentProfiles.CountAsync(p => p.SchoolId == sc.Id, ct);
            var classes  = await _uow.Classes.CountAsync(c => c.SchoolId == sc.Id, ct);
            var users    = await _uow.Users.CountAsync(u => u.SchoolId == sc.Id, ct);
            rows.Add(new SchoolAccountDto
            {
                SchoolId           = sc.Id,
                SchoolCode         = sc.SchoolCode,
                SchoolName         = sc.SchoolName,
                IsActive           = sc.IsActive,
                AdminUserId        = ad?.Id ?? 0,
                AdminFullName      = ad?.FullName,
                AdminEmail         = ad?.Email,
                AdminActive        = ad?.IsActive ?? false,
                PlanName           = sub?.Plan?.Name,
                SubscriptionStatus = sub != null ? sub.Status.ToString() : "None",
                SubscriptionActive = sub != null && sub.Status == SubscriptionStatus.Active && sub.CurrentPeriodEnd >= Today,
                UserCount          = users,
                StudentCount       = students,
                ClassCount         = classes,
                CanDelete          = students == 0 && classes == 0,
                CreatedAt          = sc.CreatedAt
            });
        }
        return Result<IReadOnlyList<SchoolAccountDto>>.Success(rows);
    }

    public async Task<Result> UpdateSchoolAsync(UpdateSchoolRequest req, CancellationToken ct = default)
    {
        var school = await _uow.Schools.GetByIdAsync(req.SchoolId, ct);
        if (school is null) return Result.Failure("Trường không tồn tại.", "NOT_FOUND");

        school.SchoolName = req.SchoolName.Trim();

        // Tracked query (FindAsync dùng AsNoTracking nên sửa sẽ không lưu)
        var admin = await _uow.Users.AsQueryable()
            .Where(u => u.SchoolId == req.SchoolId && u.Role.RoleCode == "ADMIN")
            .OrderBy(u => u.Id).FirstOrDefaultAsync(ct);
        if (admin is not null)
        {
            var email = req.AdminEmail.Trim().ToLowerInvariant();
            if (!string.Equals(email, admin.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _uow.Users.AnyAsync(u => u.SchoolId == req.SchoolId && u.Email == email && u.Id != admin.Id, ct))
                    return Result.Failure("Email đã được dùng trong trường này.", "EMAIL_EXISTS");
                admin.Email = email;
            }
            admin.FullName = req.AdminFullName.Trim();
            admin.IsActive = req.AdminActive;
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Updated school {Id} ({Name})", req.SchoolId, school.SchoolName);
        return Result.Success();
    }

    public async Task<Result> ToggleSchoolActiveAsync(int schoolId, int currentSchoolId, CancellationToken ct = default)
    {
        if (schoolId == currentSchoolId) return Result.Failure("Không thể tạm khóa trường của chính bạn.", "SELF");
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result.Failure("Trường không tồn tại.", "NOT_FOUND");

        school.IsActive = !school.IsActive;
        var users = await _uow.Users.AsQueryable().Where(u => u.SchoolId == schoolId).ToListAsync(ct);
        foreach (var u in users) u.IsActive = school.IsActive;   // chặn/mở đăng nhập theo trạng thái trường
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("School {Id} active -> {Active}", schoolId, school.IsActive);
        return Result.Success();
    }

    public async Task<Result> ResetSchoolAdminPasswordAsync(int schoolId, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return Result.Failure("Mật khẩu tối thiểu 6 ký tự.", "WEAK");

        var admin = await _uow.Users.AsQueryable()
            .Where(u => u.SchoolId == schoolId && u.Role.RoleCode == "ADMIN")
            .OrderBy(u => u.Id).FirstOrDefaultAsync(ct);
        if (admin is null) return Result.Failure("Không tìm thấy tài khoản Nhà trường.", "NOT_FOUND");

        admin.PasswordHash = _hasher.HashPassword(admin, newPassword);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Reset Nhà trường password for school {Id}", schoolId);
        return Result.Success();
    }

    /// <summary>
    /// Xóa VĨNH VIỄN một trường và TOÀN BỘ dữ liệu liên quan (cascade thủ công).
    /// Tạm tắt FK constraint, xóa mọi bảng có cột SchoolId, dọn các bản ghi con mồ côi,
    /// rồi xóa bản ghi trường và bật lại constraint. Tất cả trong 1 transaction — lỗi sẽ rollback.
    /// </summary>
    public async Task<Result> DeleteSchoolAsync(int schoolId, int currentSchoolId, CancellationToken ct = default)
    {
        if (schoolId == currentSchoolId) return Result.Failure("Không thể xóa trường của chính bạn.", "SELF");
        var school = await _uow.Schools.GetByIdAsync(schoolId, ct);
        if (school is null) return Result.Failure("Trường không tồn tại.", "NOT_FOUND");

        // Script cascade tổng quát: đọc tên bảng/cột từ sys catalog (không hardcode), an toàn với mọi
        // tên bảng. schoolId là int do hệ thống truyền — nhúng trực tiếp, không có rủi ro SQL injection.
        var sql = $@"
SET NOCOUNT ON;
DECLARE @sid INT = {schoolId};

-- 1) Tạm tắt toàn bộ FK constraint để thứ tự xóa không quan trọng
DECLARE @off NVARCHAR(MAX) = N'', @on NVARCHAR(MAX) = N'';
SELECT @off += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' NOCHECK CONSTRAINT ALL;' + CHAR(10),
       @on  += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables WHERE is_ms_shipped = 0;
EXEC sys.sp_executesql @off;

-- 2) Xóa dữ liệu ở mọi bảng có cột [SchoolId]
DECLARE @del NVARCHAR(MAX) = N'';
SELECT @del += 'DELETE FROM ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + ' WHERE [SchoolId] = @sid;' + CHAR(10)
FROM sys.tables t
WHERE t.is_ms_shipped = 0
  AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = 'SchoolId');
EXEC sys.sp_executesql @del, N'@sid INT', @sid = @sid;

-- 3) Dọn các bản ghi con mồ côi (FK trỏ tới cha đã bị xóa) — lặp nhiều lượt cho các cấp lồng nhau
DECLARE @pass INT = 0;
WHILE @pass < 8
BEGIN
    DECLARE @orph NVARCHAR(MAX) = N'';
    SELECT @orph += 'DELETE c FROM ' + QUOTENAME(SCHEMA_NAME(ct.schema_id)) + '.' + QUOTENAME(ct.name) + ' c WHERE c.'
        + QUOTENAME(pc.name) + ' IS NOT NULL AND NOT EXISTS (SELECT 1 FROM '
        + QUOTENAME(SCHEMA_NAME(rt.schema_id)) + '.' + QUOTENAME(rt.name) + ' p WHERE p.'
        + QUOTENAME(rc.name) + ' = c.' + QUOTENAME(pc.name) + ');' + CHAR(10)
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.tables ct ON fk.parent_object_id = ct.object_id
    JOIN sys.columns pc ON pc.object_id = ct.object_id AND pc.column_id = fkc.parent_column_id
    JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
    JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
    WHERE fk.parent_object_id <> fk.referenced_object_id;
    EXEC sys.sp_executesql @orph;
    SET @pass += 1;
END

-- 4) Xóa bản ghi trường (PK của bảng Schools là cột [SchoolId]). Bước 2 thường đã xóa,
--    nhưng giữ lại cho chắc — nếu đã xóa thì câu này tác động 0 dòng.
DELETE FROM [Schools] WHERE [SchoolId] = @sid;

-- 5) Bật lại toàn bộ FK constraint (validate dữ liệu còn lại)
EXEC sys.sp_executesql @on;";

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                await _uow.ExecuteRawSqlAsync(sql, ct);
            }, ct);
            _logger.LogWarning("Force-deleted school {Id} ({Name}) and all related data", schoolId, school.SchoolName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Force-delete school {Id} failed", schoolId);
            return Result.Failure("Không xóa được trường (đã hoàn tác). Vui lòng thử lại hoặc dùng 'Tạm khóa'.", "DELETE_FAILED");
        }
    }

    private static void ApplyPlan(SubscriptionPlan p, PlanEditRequest r, string code)
    {
        p.PlanCode           = code;
        p.Name               = r.Name.Trim();
        p.Description         = r.Description?.Trim();
        p.Tier               = r.Tier;
        p.MonthlyPrice       = r.MonthlyPrice;
        p.QuarterlyPrice     = r.QuarterlyPrice;
        p.YearlyPrice        = r.YearlyPrice;
        p.IncludesAiTutor    = r.IncludesAiTutor;
        p.IncludesVirtualLab = r.IncludesVirtualLab;
        p.IsActive           = r.IsActive;
        p.MaxTeachers        = r.MaxTeachers;
        p.MaxStudents        = r.MaxStudents;
        p.MaxParents         = r.MaxParents;
        p.MaxAdmins          = r.MaxAdmins;
        p.MaxAccountants     = r.MaxAccountants;
        p.MaxSupervisors     = r.MaxSupervisors;
        p.MaxClasses         = r.MaxClasses;
    }

    private static void ApplyAddOn(SubscriptionAddOn a, AddOnEditRequest r, string code)
    {
        a.AddOnCode      = code;
        a.Name           = r.Name.Trim();
        a.Description     = r.Description?.Trim();
        a.Feature        = r.Feature;
        a.MonthlyPrice   = r.MonthlyPrice;
        a.QuarterlyPrice = r.QuarterlyPrice;
        a.YearlyPrice    = r.YearlyPrice;
        a.IsActive       = r.IsActive;
    }

    private static void ApplyQuotaAddOn(RoleQuotaAddOn a, QuotaAddOnEditRequest r, string code)
    {
        a.AddOnCode      = code;
        a.Name           = r.Name.Trim();
        a.Description     = r.Description?.Trim();
        a.TargetRole     = r.TargetRole;
        a.ExtraQuota     = r.ExtraQuota;
        a.ExtraClasses   = r.ExtraClasses;
        a.MonthlyPrice   = r.MonthlyPrice;
        a.QuarterlyPrice = r.QuarterlyPrice;
        a.YearlyPrice    = r.YearlyPrice;
        a.IsActive       = r.IsActive;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SchoolSubscription?> LoadSubscriptionAsync(int schoolId, CancellationToken ct) =>
        await _uow.SchoolSubscriptions.FindOneAsync(
            s => s.SchoolId == schoolId,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.AddOns).ThenInclude(a => a.AddOn)
                           .Include(s => s.RoleQuotaAddOns).ThenInclude(a => a.AddOn)
                           .Include(s => s.Orders),
            ct: ct);

    private async Task<SubscriptionOrder> BuildOrderAsync(
        int schoolId, int userId, int subscriptionId, SubscriptionOrderType type, int? planId,
        SubscriptionCycle cycle, decimal amount, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct)
    {
        var seq = await _uow.SubscriptionOrders.CountAsync(o => o.SchoolId == schoolId, ct);
        return new SubscriptionOrder
        {
            SchoolId        = schoolId,
            SubscriptionId  = subscriptionId,
            OrderCode       = $"SUB{schoolId:D3}-{seq + 1:D4}",   // mã ngắn: trường + số thứ tự
            OrderType       = type,
            Status          = SubscriptionOrderStatus.Pending,
            PlanId          = planId,
            BillingCycle    = cycle,
            Amount          = amount,
            PeriodStart     = periodStart,
            PeriodEnd       = periodEnd,
            CreatedByUserId = userId == 0 ? null : userId
        };
    }

    /// <summary>Tạo (và Add) đơn gia hạn cho kỳ kế tiếp gồm gói + các add-on đang hoạt động.</summary>
    private async Task<SubscriptionOrder> BuildRenewalOrderAsync(SchoolSubscription sub, int userId, CancellationToken ct)
    {
        var cycle       = sub.BillingCycle;
        var periodStart = sub.CurrentPeriodEnd >= Today ? sub.CurrentPeriodEnd : Today;
        var periodEnd   = AddCycle(periodStart, cycle);

        var planPrice  = sub.Plan.PriceFor(cycle);
        var order = await BuildOrderAsync(sub.SchoolId, userId, sub.Id, SubscriptionOrderType.Renewal,
            sub.PlanId, cycle, 0M, periodStart, periodEnd, ct);

        var total = planPrice;
        order.Items.Add(new SubscriptionOrderItem
        {
            ItemType    = SubscriptionItemType.Plan,
            RefId       = sub.PlanId,
            Description = $"Gia hạn gói {sub.Plan.Name} ({CycleLabel(cycle)})",
            Amount      = planPrice
        });

        foreach (var a in sub.AddOns.Where(a => a.IsActive))
        {
            var addOnPrice = a.AddOn.PriceFor(cycle);
            total += addOnPrice;
            order.Items.Add(new SubscriptionOrderItem
            {
                ItemType    = SubscriptionItemType.AddOn,
                RefId       = a.AddOnId,
                Description = $"Gia hạn tính năng {a.AddOn.Name}",
                Amount      = addOnPrice
            });
        }

        foreach (var a in sub.RoleQuotaAddOns.Where(a => a.IsActive))
        {
            var addOnPrice = a.AddOn.PriceFor(cycle);
            total += addOnPrice;
            order.Items.Add(new SubscriptionOrderItem
            {
                ItemType    = SubscriptionItemType.QuotaAddOn,
                RefId       = a.AddOnId,
                Description = $"Gia hạn gói mở rộng {a.AddOn.Name}",
                Amount      = addOnPrice
            });
        }

        order.Amount = total;
        await _uow.SubscriptionOrders.AddAsync(order, ct);
        return order;
    }

    private static string CycleLabel(SubscriptionCycle c) => c switch
    {
        SubscriptionCycle.Monthly   => "theo tháng",
        SubscriptionCycle.Quarterly => "theo quý",
        SubscriptionCycle.Yearly    => "theo năm",
        _                           => c.ToString()
    };

    private static CurrentSubscriptionDto MapCurrent(SchoolSubscription? sub)
    {
        if (sub is null) return new CurrentSubscriptionDto { HasSubscription = false };

        var isActive = sub.Status == SubscriptionStatus.Active && sub.CurrentPeriodEnd >= Today;
        var hasAi  = isActive && (sub.Plan.IncludesAiTutor    || sub.AddOns.Any(a => a.IsActive && a.ActiveUntil >= Today && a.AddOn.Feature == PremiumFeature.AiTutor));
        var hasLab = isActive && (sub.Plan.IncludesVirtualLab || sub.AddOns.Any(a => a.IsActive && a.ActiveUntil >= Today && a.AddOn.Feature == PremiumFeature.VirtualLab));

        return new CurrentSubscriptionDto
        {
            HasSubscription  = true,
            SubscriptionId   = sub.Id,
            PlanCode         = sub.Plan.PlanCode,
            PlanName         = sub.Plan.Name,
            Tier             = sub.Plan.Tier,
            Status           = sub.Status.ToString(),
            BillingCycle     = sub.BillingCycle.ToString(),
            StartDate        = sub.StartDate,
            CurrentPeriodEnd = sub.CurrentPeriodEnd,
            AutoRenew        = sub.AutoRenew,
            IsActive         = isActive,
            DaysRemaining    = Math.Max(0, sub.CurrentPeriodEnd.DayNumber - Today.DayNumber),
            HasAiTutor       = hasAi,
            HasVirtualLab    = hasLab,
            ActiveAddOns     = sub.AddOns
                .Where(a => a.IsActive && a.ActiveUntil >= Today)
                .Select(a => new ActiveAddOnDto
                {
                    AddOnId     = a.AddOnId,
                    Name        = a.AddOn.Name,
                    Feature     = a.AddOn.Feature.ToString(),
                    ActiveUntil = a.ActiveUntil
                }).ToList()
        };
    }

    private static SubscriptionOrderDto MapOrder(SubscriptionOrder o, Domain.Entities.Subscription.SubscriptionPlan? planOverride = null)
    {
        var planName = o.Plan?.Name ?? planOverride?.Name;
        return new SubscriptionOrderDto
        {
            OrderId       = o.Id,
            OrderCode     = o.OrderCode,
            OrderType     = o.OrderType.ToString(),
            Status        = o.Status.ToString(),
            PlanName      = planName,
            BillingCycle  = o.BillingCycle.ToString(),
            Amount        = o.Amount,
            PeriodStart   = o.PeriodStart,
            PeriodEnd     = o.PeriodEnd,
            PaymentMethod = o.PaymentMethod?.ToString(),
            CreatedAt     = o.CreatedAt,
            PaidAt        = o.PaidAt,
            Items         = o.Items.Select(i => new SubscriptionOrderItemDto
            {
                ItemType    = i.ItemType.ToString(),
                Description = i.Description,
                Amount      = i.Amount
            }).ToList()
        };
    }
}
