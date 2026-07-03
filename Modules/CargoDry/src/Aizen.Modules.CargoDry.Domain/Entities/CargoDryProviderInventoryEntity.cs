using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Provider Inventory entity",
    "Tracks CargoDry kit stock held by a specific provider, grouped by provider + product + batch. " +
    "One row per (ProviderProfileId, ProductCode, BatchCode) combination. " +
    "AvailableStock is computed as TotalAllocated + TotalAdjusted - TotalActivated - TotalRevoked - TotalReturned. " +
    "Phase 2 (July 2026): Created as part of CargoDry provider inventory foundation.")]
public sealed class CargoDryProviderInventoryEntity : AizenEntityWithAudit
{
    // ── Identity ───────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Identity/Profile. No EF FK constraint.</summary>
    public long    ProviderProfileId  { get; private set; }

    /// <summary>Which CargoDry product this inventory row tracks.</summary>
    public string  ProductCode        { get; private set; } = default!;

    /// <summary>
    /// Optional batch code. One row per allocated batch per provider.
    /// Null = aggregate row (not used in Phase 2; batch-level rows only).
    /// </summary>
    public string? BatchCode          { get; private set; }

    // ── Commercial context ─────────────────────────────────────────────────────
    public CargoDryCommercialModel CommercialModel  { get; private set; }
    public SalesChannel            SalesChannel     { get; private set; }
    public StockLocationType       StockLocationType { get; private set; }

    // ── Stock counters ─────────────────────────────────────────────────────────
    /// <summary>Number of kits allocated to the provider via this batch.</summary>
    public int TotalAllocated  { get; private set; }

    /// <summary>Number of kits that have been activated by end users (leave provider pool).</summary>
    public int TotalActivated  { get; private set; }

    /// <summary>Number of kits revoked by admin while still in the provider's pool.</summary>
    public int TotalRevoked    { get; private set; }

    /// <summary>Number of kits returned from the provider back to Inktavia's warehouse.</summary>
    public int TotalReturned   { get; private set; }

    /// <summary>
    /// Net cumulative manual adjustment quantity.
    /// Positive = admin added stock; negative = admin removed stock.
    /// Starts at 0 at allocation time.
    /// </summary>
    public int TotalAdjusted   { get; private set; }

    // ── Tracking ───────────────────────────────────────────────────────────────
    public DateTime? LastMovementAtUtc { get; private set; }
    public DateTime  CreatedAtUtc      { get; private set; }
    public DateTime? UpdatedAtUtc      { get; private set; }

    // ── Computed — NOT mapped to DB ────────────────────────────────────────────
    /// <summary>
    /// Available (un-activated, un-revoked, un-returned) kits in the provider's possession.
    /// Must never be negative.
    /// </summary>
    public int AvailableStock
        => TotalAllocated + TotalAdjusted - TotalActivated - TotalRevoked - TotalReturned;

    private CargoDryProviderInventoryEntity() { }

    // ── Factory ────────────────────────────────────────────────────────────────
    public static CargoDryProviderInventoryEntity Create(
        long                   providerProfileId,
        string                 productCode,
        string?                batchCode,
        CargoDryCommercialModel commercialModel,
        SalesChannel           salesChannel,
        StockLocationType      stockLocationType,
        int                    initialAllocated,
        DateTime               nowUtc)
    {
        if (initialAllocated <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialAllocated),
                "Initial allocated count must be positive.");

        return new()
        {
            ProviderProfileId  = providerProfileId,
            ProductCode        = productCode,
            BatchCode          = batchCode,
            CommercialModel    = commercialModel,
            SalesChannel       = salesChannel,
            StockLocationType  = stockLocationType,
            TotalAllocated     = initialAllocated,
            TotalActivated     = 0,
            TotalRevoked       = 0,
            TotalReturned      = 0,
            TotalAdjusted      = 0,
            LastMovementAtUtc  = nowUtc,
            CreatedAtUtc       = nowUtc,
            IsActive           = true,
        };
    }

    // ── Domain mutation methods ────────────────────────────────────────────────

    /// <summary>
    /// Increases the allocation count (e.g. additional batch assigned to same provider+product).
    /// </summary>
    public void IncreaseAllocation(int count, DateTime nowUtc)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Allocation increment must be positive.");

        TotalAllocated    += count;
        LastMovementAtUtc  = nowUtc;
        UpdatedAtUtc       = nowUtc;
    }

    /// <summary>
    /// Records kit activation. Decreases available stock by one.
    /// </summary>
    public void IncrementActivated(int count, DateTime nowUtc)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Activated count increment must be positive.");
        if (TotalActivated + count > TotalAllocated + TotalAdjusted - TotalRevoked - TotalReturned)
            throw new InvalidOperationException(
                $"Cannot activate {count} kit(s): would exceed available stock.");

        TotalActivated    += count;
        LastMovementAtUtc  = nowUtc;
        UpdatedAtUtc       = nowUtc;
    }

    /// <summary>
    /// Records kit revoke while in provider's pool. Decreases available stock.
    /// </summary>
    public void IncrementRevoked(int count, DateTime nowUtc)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Revoked count increment must be positive.");
        if (AvailableStock < count)
            throw new InvalidOperationException(
                $"Cannot revoke {count} kit(s): available stock is only {AvailableStock}.");

        TotalRevoked      += count;
        LastMovementAtUtc  = nowUtc;
        UpdatedAtUtc       = nowUtc;
    }

    /// <summary>
    /// Records kit return from provider back to Inktavia. Decreases available stock.
    /// </summary>
    public void IncrementReturned(int count, DateTime nowUtc)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Returned count increment must be positive.");
        if (AvailableStock < count)
            throw new InvalidOperationException(
                $"Cannot return {count} kit(s): available stock is only {AvailableStock}.");

        TotalReturned     += count;
        LastMovementAtUtc  = nowUtc;
        UpdatedAtUtc       = nowUtc;
    }

    /// <summary>
    /// Admin manual adjustment. Positive = add stock; negative = remove stock.
    /// Negative adjustments cannot make AvailableStock negative.
    /// </summary>
    public void Adjust(int quantity, DateTime nowUtc)
    {
        if (quantity == 0)
            throw new ArgumentException("Adjustment quantity must be non-zero.", nameof(quantity));
        if (AvailableStock + quantity < 0)
            throw new InvalidOperationException(
                $"Adjustment of {quantity} would make AvailableStock negative " +
                $"(current: {AvailableStock}). Adjustment rejected.");

        TotalAdjusted     += quantity;
        LastMovementAtUtc  = nowUtc;
        UpdatedAtUtc       = nowUtc;
    }
}
