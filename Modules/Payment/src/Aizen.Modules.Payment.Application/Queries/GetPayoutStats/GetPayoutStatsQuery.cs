using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutStats;

public sealed class GetPayoutStatsQuery : AizenQuery<PayoutStatsDto> { }

public sealed record PayoutStatsDto(
    decimal PendingTotal,
    int     PendingCount,
    decimal PaidThisMonth,
    int     PaidThisMonthCount,
    decimal OnHoldAmount,
    int     OnHoldCount
);
