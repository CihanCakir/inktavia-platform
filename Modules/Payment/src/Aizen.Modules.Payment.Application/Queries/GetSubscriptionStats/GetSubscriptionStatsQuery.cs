using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionStats;

public sealed class GetSubscriptionStatsQuery : AizenQuery<SubscriptionStatsDto> { }

public sealed record SubscriptionStatsDto(
    int     ActiveProviderSubscriptions,
    int     ActiveParticipantSubscriptions,
    decimal MonthlyRecurringRevenue,
    int     FailedRenewalsThisMonth,
    int     ExpiringSoonCount
);
