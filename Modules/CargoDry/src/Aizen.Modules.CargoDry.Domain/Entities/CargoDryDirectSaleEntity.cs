using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Direct Sale entity",
    "CargoDry supply v2 (cargo / direct online sale): records the RETAIL revenue of a platform-fulfilled CargoDry order " +
    "for which NO program provider accepted (fallback → cargo shipment). There is no provider and no commission, so it " +
    "never enters the provider-keyed SellThroughSettlement. It is the CargoDry-domain revenue record for cargo sales — " +
    "finance/reporting must union CargoDrySalesAttribution (provider sales) with this table for total retail revenue. " +
    "Idempotent per source SR (SourceServiceRequestId unique).")]
public sealed class CargoDryDirectSaleEntity : AizenEntityWithAudit
{
    /// <summary>Source CARGODRY_SUPPLY service request (idempotency key — one direct-sale record per cargo order).</summary>
    public long    SourceServiceRequestId { get; private set; }
    public string  ProductCode            { get; private set; } = default!;
    /// <summary>Retail amount the owner paid (platform revenue).</summary>
    public decimal SaleAmount             { get; private set; }
    public string  CurrencyCode           { get; private set; } = default!;
    /// <summary>Optional: the shipped kit id, when admin linked one at mark-shipped.</summary>
    public long?   KitId                  { get; private set; }
    public string? TrackingCode           { get; private set; }
    public DateTime? ShippedAtUtc         { get; private set; }
    public DateTime  CompletedAtUtc       { get; private set; }

    private CargoDryDirectSaleEntity() { }

    public static CargoDryDirectSaleEntity Create(
        long sourceServiceRequestId, string productCode, decimal saleAmount, string currencyCode,
        DateTime completedAtUtc, long? kitId = null, string? trackingCode = null, DateTime? shippedAtUtc = null)
        => new()
        {
            SourceServiceRequestId = sourceServiceRequestId,
            ProductCode            = productCode.ToUpperInvariant(),
            SaleAmount             = saleAmount,
            CurrencyCode           = currencyCode.ToUpperInvariant(),
            KitId                  = kitId,
            TrackingCode           = trackingCode,
            ShippedAtUtc           = shippedAtUtc,
            CompletedAtUtc         = completedAtUtc,
            IsActive               = true,
        };
}
