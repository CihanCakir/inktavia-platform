using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderEarnings;

public sealed class GetCargoDryProviderEarningsQueryHandler
    : AizenQueryHandler<GetCargoDryProviderEarningsQuery, CargoDryProviderEarningsDto>
{
    private const decimal TargetFloor = 150m;
    private readonly ICargoDrySalesAttributionRepository _attributions;
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDryProviderInventoryRepository _inventory;
    private readonly ICargoDryKitRepository _kits;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryProviderEarningsQueryHandler(
        ICargoDrySalesAttributionRepository attributions,
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDryProviderInventoryRepository inventory,
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products)
    {
        _attributions = attributions;
        _settlements = settlements;
        _inventory = inventory;
        _kits = kits;
        _products = products;
    }

    public override async Task<CargoDryProviderEarningsDto?> Handle(
        GetCargoDryProviderEarningsQuery request, CancellationToken ct)
    {
        var pid = request.ProviderProfileId;
        var now = DateTimeOffset.UtcNow;

        // Commission aggregation
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var yearStart = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endOfTime = now.AddDays(1); // future boundary

        var thisMonthCommission = await _attributions.SumProviderCommissionAsync(pid, monthStart, endOfTime, ct);
        var ytdCommission = await _attributions.SumProviderCommissionAsync(pid, yearStart, endOfTime, ct);

        // Settlement payouts
        var pendingStatuses = new[]
        {
            CargoDrySellThroughSettlementStatus.Pending,
            CargoDrySellThroughSettlementStatus.ReadyForSettlement,
            CargoDrySellThroughSettlementStatus.Scheduled,
        };
        var pendingPayout = await _settlements.SumProviderPayoutByStatusAsync(pid, pendingStatuses, ct);
        var paidPayout = await _settlements.SumProviderPayoutByStatusAsync(pid,
            new[] { CargoDrySellThroughSettlementStatus.Settled }, ct);

        // Inventory
        var inventoryRows = await _inventory.GetByProviderAsync(pid, ct);
        var soldKits = inventoryRows.Sum(i => i.TotalActivated);
        var inHandKits = inventoryRows.Sum(i => i.AvailableStock);
        var totalAllocated = inventoryRows.Sum(i => i.TotalAllocated);
        var sellThroughPct = totalAllocated > 0
            ? Math.Round((decimal)soldKits / totalAllocated * 100, 1)
            : 0m;

        // Product lookup for potential calculations
        var allProducts = await _products.GetAllActiveAsync(ct);
        var productLookup = allProducts.ToDictionary(p => p.ProductCode, p => p);

        // InHandPotential: sum of (each inventory row's AvailableStock × product earning)
        var inHandPotential = 0m;
        foreach (var inv in inventoryRows)
        {
            if (inv.AvailableStock <= 0) continue;
            if (productLookup.TryGetValue(inv.ProductCode, out var product))
            {
                var earning = product.ProviderEarningPerSale();
                if (earning is > 0m)
                    inHandPotential += inv.AvailableStock * earning.Value;
            }
        }

        // RenewalPotential: expiring kits × product earning
        var expiringKits = await _kits.GetExpiringAsync(90, pid, ct);
        var renewalPotential = 0m;
        foreach (var kit in expiringKits)
        {
            if (productLookup.TryGetValue(kit.ProductCode, out var product))
            {
                var earning = product.ProviderEarningPerSale();
                if (earning is > 0m)
                    renewalPotential += earning.Value;
            }
        }

        var avgEarningPerKit = soldKits > 0 ? decimal.Round(ytdCommission / soldKits, 2) : 0m;

        // ── Monthly target (trailing 3-month average × 1.10, floor) ──────────
        var m1Start = monthStart.AddMonths(-1);
        var m2Start = monthStart.AddMonths(-2);
        var m3Start = monthStart.AddMonths(-3);

        var m1Commission = await _attributions.SumProviderCommissionAsync(pid, m3Start, m2Start, ct);
        var m2Commission = await _attributions.SumProviderCommissionAsync(pid, m2Start, m1Start, ct);
        var m3Commission = await _attributions.SumProviderCommissionAsync(pid, m1Start, monthStart, ct);

        var avg3 = (m1Commission + m2Commission + m3Commission) / 3m;
        var monthlyTarget = Math.Max(Math.Round(avg3 * 1.10m, 0), TargetFloor);
        var targetAchieved = thisMonthCommission;
        var remainingToTarget = Math.Max(monthlyTarget - targetAchieved, 0m);
        var progressPct = monthlyTarget > 0
            ? Math.Clamp(Math.Round(targetAchieved * 100m / monthlyTarget, 1), 0m, 100m)
            : 0m;

        // Currency: use first product's currency or default
        var currency = allProducts.FirstOrDefault()?.CurrencyCode ?? "USD";

        return new CargoDryProviderEarningsDto
        {
            ThisMonthCommission = thisMonthCommission,
            YtdCommission = ytdCommission,
            PendingPayout = pendingPayout,
            PaidPayout = paidPayout,
            InHandPotential = inHandPotential,
            RenewalPotential = renewalPotential,
            AvgEarningPerKit = avgEarningPerKit,
            SellThroughPct = sellThroughPct,
            SoldKits = soldKits,
            InHandKits = inHandKits,
            MonthlyTarget = monthlyTarget,
            TargetAchieved = targetAchieved,
            RemainingToTarget = remainingToTarget,
            ProgressPct = progressPct,
            CurrencyCode = currency,
            ComputedAtUtc = now,
        };
    }
}
