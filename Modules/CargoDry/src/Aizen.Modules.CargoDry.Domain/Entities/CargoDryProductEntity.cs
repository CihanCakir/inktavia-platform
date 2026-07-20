using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Product entity",
    "Defines a moisture protection kit product variant (e.g., Standard-90, Premium-180). " +
    "Product catalog is managed by admin. ValidityDays drives kit expiry calculation on activation. " +
    "Phase 0 (July 2026): Added WholesalePrice, ConsignmentPrice, ProviderCommissionRate.")]
public sealed class CargoDryProductEntity : AizenEntityWithAudit
{
    public string  ProductCode    { get; private set; } = default!;
    public string  Name           { get; private set; } = default!;
    public string  Description    { get; private set; } = default!;
    public int     ValidityDays   { get; private set; }
    public bool    HasSmartDevice { get; private set; }
    /// <summary>Optional device variant identifier (e.g. "AQUASENSE-V5"). Required when HasSmartDevice is true.</summary>
    public string? DeviceType     { get; private set; }
    public decimal RetailPrice    { get; private set; }
    public string  CurrencyCode   { get; private set; } = default!;

    // IsActive inherited from AizenEntityWithAudit — do NOT redeclare
    // CreateDate inherited from AizenEntityWithAudit — do NOT redeclare

    // ── Commercial pricing (Phase 0, July 2026) ───────────────────────────────
    /// <summary>
    /// Price charged to a reselling provider (ProviderResale channel).
    /// Provider pays this upfront at batch purchase; end buyer pays RetailPrice to provider.
    /// Null = not configured for resale.
    /// </summary>
    public decimal? WholesalePrice { get; private set; }

    /// <summary>
    /// Reference price used to calculate consignment settlement amounts.
    /// Typically equals RetailPrice but can be overridden.
    /// ProviderShareAmount = ConsignmentPrice × ProviderCommissionRate.
    /// Null = uses RetailPrice as fallback.
    /// </summary>
    public decimal? ConsignmentPrice { get; private set; }

    /// <summary>
    /// Provider's share rate for ProviderAttributedSale and ConsignmentSellThrough channels.
    /// Range: 0.00–1.00 (e.g. 0.20 = 20%).
    /// Null = no provider commission configured for this product.
    /// Decision N9: configurable first-sale referral/commission.
    /// </summary>
    public decimal? ProviderCommissionRate { get; private set; }

    private CargoDryProductEntity() { }

    public static CargoDryProductEntity Create(
        string productCode, string name, string description,
        int validityDays, decimal retailPrice, string currencyCode,
        bool hasSmartDevice = false, string? deviceType = null,
        decimal? wholesalePrice = null, decimal? consignmentPrice = null,
        decimal? providerCommissionRate = null)
        => new()
        {
            ProductCode            = productCode.ToUpperInvariant(),
            Name                   = name,
            Description            = description,
            ValidityDays           = validityDays,
            RetailPrice            = retailPrice,
            CurrencyCode           = currencyCode.ToUpperInvariant(),
            HasSmartDevice         = hasSmartDevice,
            DeviceType             = deviceType,
            WholesalePrice         = wholesalePrice,
            ConsignmentPrice       = consignmentPrice,
            ProviderCommissionRate = providerCommissionRate,
            IsActive               = true,
        };

    public void SetActive(bool active) => IsActive = active;
    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void UpdatePrice(decimal price) => RetailPrice = price;

    public void Update(
        string name, string description, int validityDays,
        decimal retailPrice, string currencyCode,
        bool hasSmartDevice, string? deviceType)
    {
        Name           = name;
        Description    = description;
        ValidityDays   = validityDays;
        RetailPrice    = retailPrice;
        CurrencyCode   = currencyCode.ToUpperInvariant();
        HasSmartDevice = hasSmartDevice;
        DeviceType     = deviceType;
    }

    /// <summary>
    /// Provider earning per single sale: (ConsignmentPrice ?? RetailPrice) × ProviderCommissionRate, rounded to 2 dp.
    /// Returns null when no commission rate is configured.
    /// </summary>
    public decimal? ProviderEarningPerSale()
        => ProviderCommissionRate is > 0m
            ? decimal.Round((ConsignmentPrice ?? RetailPrice) * ProviderCommissionRate.Value, 2)
            : null;

    /// <summary>
    /// Updates commercial pricing fields. Null values are applied as-is (clearing the field).
    /// </summary>
    public void UpdateCommercialPricing(
        decimal? wholesalePrice,
        decimal? consignmentPrice,
        decimal? providerCommissionRate)
    {
        if (providerCommissionRate.HasValue && (providerCommissionRate < 0 || providerCommissionRate > 1))
            throw new InvalidOperationException("ProviderCommissionRate must be between 0.00 and 1.00.");

        WholesalePrice         = wholesalePrice;
        ConsignmentPrice       = consignmentPrice;
        ProviderCommissionRate = providerCommissionRate;
    }
}
