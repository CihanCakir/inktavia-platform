using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutStats;

[DocumentationInfo("GetPayoutStatsQueryHandler",
    "Computes payout KPI stats for the admin dashboard strip: pending total, paid this month, on-hold. " +
    "Loads relevant payout pages from the repository and aggregates in-memory.")]
public sealed class GetPayoutStatsQueryHandler
    : AizenQueryHandler<GetPayoutStatsQuery, PayoutStatsDto>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetPayoutStatsQueryHandler(IPayoutRecordRepository payouts)
        => _payouts = payouts;

    public override async Task<PayoutStatsDto?> Handle(
        GetPayoutStatsQuery request, CancellationToken ct)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Pending payouts
        var (pendingItems, _) = await _payouts.GetPagedAsync(
            PayoutStatus.Pending, null, null, null, 0, 10_000, ct);

        // On-hold payouts
        var (holdItems, _) = await _payouts.GetPagedAsync(
            PayoutStatus.OnHold, null, null, null, 0, 10_000, ct);

        // Completed this month
        var (completedItems, _) = await _payouts.GetPagedAsync(
            PayoutStatus.Completed, null, monthStart, now, 0, 10_000, ct);

        return new PayoutStatsDto(
            PendingTotal:      pendingItems.Sum(p => p.Amount),
            PendingCount:      pendingItems.Count,
            PaidThisMonth:     completedItems.Sum(p => p.Amount),
            PaidThisMonthCount: completedItems.Count,
            OnHoldAmount:      holdItems.Sum(p => p.Amount),
            OnHoldCount:       holdItems.Count
        );
    }
}
