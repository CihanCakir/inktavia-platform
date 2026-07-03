using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryKitDto
{
    public long              Id                { get; init; }
    public string            SerialNumber      { get; init; } = default!;
    public string            KitCode           { get; init; } = default!;
    public string            ProductCode       { get; init; } = default!;
    public string            ProductName       { get; init; } = default!;
    public string            BatchCode         { get; init; } = default!;
    public CargoDryKitStatus Status            { get; init; }
    public long?             OwnerUserId       { get; init; }
    public string?           OwnerDisplayName  { get; init; }
    public long?             VesselId          { get; init; }
    public string?           VesselName        { get; init; }
    public DateTimeOffset?   ActivatedAt       { get; init; }
    public DateTimeOffset?   ExpiresAt         { get; init; }
    public double            EfficiencyPercent { get; init; }
    public int               DaysUntilExpiry   { get; init; }
    public int               RenewalCount      { get; init; }
    public DateTimeOffset    ManufacturedAt    { get; init; }

    // ── Commercial foundation (Phase 0, July 2026) ────────────────────────────
    /// <summary>Provider that sold or attributed this kit. Null = no attribution.</summary>
    public long?                    ProviderProfileId    { get; init; }
    /// <summary>Sales channel. Null = CommercialReviewRequired (Decision N18).</summary>
    public SalesChannel?            SalesChannel         { get; init; }
    /// <summary>Commercial model for revenue recognition. Null until attributed.</summary>
    public CargoDryCommercialModel? CommercialModel      { get; init; }
    /// <summary>Current physical/logical location in the inventory chain.</summary>
    public StockLocationType        StockLocationType    { get; init; }
    /// <summary>Cross-module Payment invoice reference (Id only, no EF FK).</summary>
    public long?                    InvoiceId            { get; init; }
    /// <summary>Cross-module Payment transaction reference (Id only, no EF FK).</summary>
    public long?                    PaymentTransactionId { get; init; }
    /// <summary>Cross-module Warehouse reference (Id only, no EF FK). Null = platform stock or activated kit. Phase 6 FK.</summary>
    public long?                    WarehouseId          { get; init; }
}
