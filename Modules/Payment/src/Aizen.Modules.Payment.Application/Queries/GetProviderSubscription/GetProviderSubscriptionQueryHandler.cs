using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSubscription;

/// <summary>
/// Provider active-subscription read-model. BE-P4: additionally resolves the versioned <c>ProviderPlanPrice</c> active now
/// (launch-vs-list label + effective window) and flags an upcoming renewal price change. Additive — existing fields unchanged.
/// </summary>
public sealed class GetProviderSubscriptionQueryHandler
    : AizenQueryHandler<GetProviderSubscriptionQuery, ProviderSubscriptionDto>
{
    private readonly IProviderPlanRepository       _repo;
    private readonly IProviderPlanPriceRepository  _prices;

    public GetProviderSubscriptionQueryHandler(IProviderPlanRepository repo, IProviderPlanPriceRepository prices)
    {
        _repo   = repo;
        _prices = prices;
    }

    public override async Task<ProviderSubscriptionDto?> Handle(
        GetProviderSubscriptionQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var sub = await _repo.GetActiveSubscriptionAsync(request.ProviderProfileId, now, ct);
        if (sub is null) return null;

        var plan = await _repo.GetByIdAsync(sub.ProviderPlanId, ct);
        var daysRemaining = Math.Max(0, (int)(sub.SubscriptionPeriodEnd - now).TotalDays);

        // BE-P4: the versioned price active NOW for this plan (Monthly) + an upcoming renewal-price change flag.
        var activeNow = await _prices.ResolveAsync(sub.ProviderPlanId, sub.CurrencyCode, BillingPeriod.Monthly, now, ct);
        var renewalPrice = await _prices.ResolveRenewalPriceAsync(sub, sub.SubscriptionPeriodEnd, ct);
        var hasUpcoming = renewalPrice is not null && renewalPrice.PriceAmount != sub.PaidAmount;

        return new ProviderSubscriptionDto
        {
            SubscriptionId  = sub.Id,
            PlanId          = sub.ProviderPlanId,
            PlanCode        = plan?.PlanCode ?? "UNKNOWN",
            PlanName        = plan?.Name ?? "Unknown",
            Status          = (int)sub.Status,
            PaidAmount      = sub.PaidAmount,
            CurrencyCode    = sub.CurrencyCode,
            MonthlyPriceTRY = plan?.MonthlyPriceTRY ?? 0,
            PeriodStart     = sub.SubscriptionPeriodStart,
            PeriodEnd       = sub.SubscriptionPeriodEnd,
            AutoRenew       = sub.AutoRenew,
            DaysRemaining   = daysRemaining,
            IsExpiringSoon  = sub.Status == SubscriptionStatus.Active && daysRemaining <= 7,
            CommissionRateAtSubscription = sub.CommissionRateAtSubscription,
            CancelledAt     = sub.CancelledAt,
            ActivePrice            = MapActivePrice(activeNow),
            HasUpcomingPriceChange = hasUpcoming,
            UpcomingPriceAmount    = hasUpcoming ? renewalPrice!.PriceAmount : null,
            UpcomingPriceChangeAt  = hasUpcoming ? sub.SubscriptionPeriodEnd : null,
        };
    }

    internal static ProviderPlanActivePriceDto? MapActivePrice(ProviderPlanPriceEntity? p)
        => p is null ? null : new ProviderPlanActivePriceDto
        {
            PriceType     = p.PriceType.ToString(),   // "Launch" | "List"
            BillingPeriod = p.BillingPeriod.ToString(),
            PriceAmount   = p.PriceAmount,
            CurrencyCode  = p.CurrencyCode,
            EffectiveFrom = p.EffectiveFrom,
            EffectiveTo   = p.EffectiveTo,
            PriceCode     = p.PriceCode,
        };
}
