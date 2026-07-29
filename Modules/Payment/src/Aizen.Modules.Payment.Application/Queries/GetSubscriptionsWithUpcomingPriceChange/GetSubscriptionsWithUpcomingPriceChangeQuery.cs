using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionsWithUpcomingPriceChange;

/// <summary>
/// BE-P4 (§13.2): active auto-renewing subscriptions whose price at the next renewal differs from the current
/// snapshot (PaidAmount), renewing within <see cref="WithinDays"/> days. Feeds Notification N1 (P4 exposes the
/// data; N1 sends the notice). Read-only — sends nothing.
/// </summary>
public sealed class GetSubscriptionsWithUpcomingPriceChangeQuery
    : AizenQuery<List<UpcomingPriceChangeItem>>
{
    public int WithinDays { get; init; } = 14;
}

public sealed record UpcomingPriceChangeItem(
    long     SubscriptionId,
    long     ProviderProfileId,
    long     ProviderPlanId,
    string   CurrencyCode,
    decimal  CurrentPaidAmount,
    decimal  UpcomingPriceAmount,
    DateTime RenewalDateUtc);
