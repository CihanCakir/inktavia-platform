using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSubscription;

public sealed class GetProviderSubscriptionQueryHandler
    : AizenQueryHandler<GetProviderSubscriptionQuery, ProviderSubscriptionDto>
{
    private readonly IProviderPlanRepository _repo;

    public GetProviderSubscriptionQueryHandler(IProviderPlanRepository repo) => _repo = repo;

    public override async Task<ProviderSubscriptionDto?> Handle(
        GetProviderSubscriptionQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var sub = await _repo.GetActiveSubscriptionAsync(request.ProviderProfileId, now, ct);
        if (sub is null) return null;

        var plan = await _repo.GetByIdAsync(sub.ProviderPlanId, ct);
        var daysRemaining = Math.Max(0, (int)(sub.SubscriptionPeriodEnd - now).TotalDays);

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
            IsExpiringSoon  = sub.Status == Aizen.Modules.Payment.Abstraction.Enum.SubscriptionStatus.Active && daysRemaining <= 7,
            CommissionRateAtSubscription = sub.CommissionRateAtSubscription,
            CancelledAt     = sub.CancelledAt,
        };
    }
}
