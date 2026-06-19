using LuminaTutors.Application.DTOs.Subscription;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Subscription;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
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
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IUnitOfWork uow, ILogger<SubscriptionService> logger)
    {
        _uow    = uow;
        _logger = logger;
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
            IsUpgrade          = current.IsActive && p.Tier > currentTier
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

        return Result<SubscriptionOverviewDto>.Success(new SubscriptionOverviewDto
        {
            Current      = current,
            Plans        = planDtos,
            AddOns       = addOnDtos,
            RecentOrders = orders
        });
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

    // ─── Confirm payment (idempotent) ─────────────────────────────────────────

    public async Task<SubscriptionConfirmResult> ConfirmOrderPaymentAsync(
        int orderId, decimal paidAmount, PaymentMethod method,
        string? transactionCode, string? gatewayRaw, CancellationToken ct = default)
    {
        var order = await _uow.SubscriptionOrders.FindOneAsync(
            o => o.Id == orderId,
            include: q => q.Include(o => o.Items)
                           .Include(o => o.Subscription).ThenInclude(s => s.Plan)
                           .Include(o => o.Subscription).ThenInclude(s => s.AddOns),
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
                            foreach (var a in sub.AddOns.Where(a => a.IsActive))
                                a.ActiveUntil = order.PeriodEnd;
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
                        break;
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Subscription order {Id} confirmed paid ({Method}) for school {School}",
                orderId, method, order.SchoolId);
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

    // ─── Process due renewals (cron) ──────────────────────────────────────────

    public async Task<int> ProcessDueRenewalsAsync(CancellationToken ct = default)
    {
        var dueSubs = await _uow.SchoolSubscriptions.FindAsync(
            s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd < Today,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.AddOns).ThenInclude(a => a.AddOn)
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

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SchoolSubscription?> LoadSubscriptionAsync(int schoolId, CancellationToken ct) =>
        await _uow.SchoolSubscriptions.FindOneAsync(
            s => s.SchoolId == schoolId,
            include: q => q.Include(s => s.Plan)
                           .Include(s => s.AddOns).ThenInclude(a => a.AddOn)
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
            OrderCode       = $"SUB{schoolId:D3}-{DateTime.UtcNow:yyyyMM}-{seq + 1:D4}",
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
