namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Rich operational KPI snapshot for the CargoDry admin dashboard.
/// All values are computed at query time — no financial values included.
/// Phase 9 (July 2026).
/// </summary>
public sealed class CargoDryOperationalOverviewDto
{
    // ── Kit lifecycle counts ──────────────────────────────────────────────────
    public int TotalKits                   { get; init; }
    public int AvailableKits               { get; init; }
    public int ActivatedKits               { get; init; }
    public int ExpiredKits                 { get; init; }
    public int RevokedKits                 { get; init; }
    public int LostKits                    { get; init; } // placeholder (no Lost status yet)
    public int RenewalDueSoonKits          { get; init; } // expiring within 30 days
    public int CommercialReviewRequiredKits{ get; init; }
    public int ProviderHeldKits            { get; init; } // StockLocationType = ProviderWarehouse
    public int WarehouseStockKits          { get; init; } // StockLocationType = PlatformWarehouse

    // ── Batch counts ──────────────────────────────────────────────────────────
    public int TotalBatches              { get; init; }
    public int ActiveBatches             { get; init; }
    public int ProviderAllocatedBatches  { get; init; }

    // ── Activity indicators ───────────────────────────────────────────────────
    public int RecentLifecycleEventCount { get; init; } // last 24 hours
    public int OpenOperationalAlertCount { get; init; } // derived from alert logic

    // ── Metadata ──────────────────────────────────────────────────────────────
    public DateTimeOffset ComputedAtUtc { get; init; }
}
