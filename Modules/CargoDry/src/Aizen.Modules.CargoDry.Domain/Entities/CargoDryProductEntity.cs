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

    // ── Media (CargoDry supply flow, additive) ────────────────────────────────
    /// <summary>
    /// FileStorage file id used as the product thumbnail. Null = no thumbnail set (BFF falls back to first image).
    /// Resolved to a presigned read URL at the BFF boundary — never stored as a URL.
    /// </summary>
    public Guid? ThumbnailFileId { get; private set; }

    private readonly List<CargoDryProductImageEntity> _images = new();
    /// <summary>Ordered gallery images (by <see cref="CargoDryProductImageEntity.SortOrder"/>).</summary>
    public IReadOnlyCollection<CargoDryProductImageEntity> Images => _images.AsReadOnly();

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

    // ── Media management (additive) ────────────────────────────────────────────

    /// <summary>Appends an image to the end of the gallery. No-op if the file id is already present.</summary>
    public CargoDryProductImageEntity? AddImage(Guid fileId)
    {
        if (fileId == Guid.Empty) throw new InvalidOperationException("Image fileId cannot be empty.");
        if (_images.Any(i => i.FileId == fileId)) return null;
        var nextOrder = _images.Count == 0 ? 0 : _images.Max(i => i.SortOrder) + 1;
        var image = CargoDryProductImageEntity.Create(Id, fileId, nextOrder);
        _images.Add(image);
        return image;
    }

    /// <summary>Removes an image by file id. If it was the thumbnail, clears the thumbnail. Returns the removed entity (for repository delete) or null.</summary>
    public CargoDryProductImageEntity? RemoveImage(Guid fileId)
    {
        var image = _images.FirstOrDefault(i => i.FileId == fileId);
        if (image is null) return null;
        _images.Remove(image);
        if (ThumbnailFileId == fileId) ThumbnailFileId = null;
        // Compact remaining sort orders to stay contiguous.
        var ordered = _images.OrderBy(i => i.SortOrder).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].SetSortOrder(i);
        return image;
    }

    /// <summary>
    /// Reorders the gallery to match <paramref name="orderedFileIds"/>. Every current image id must appear exactly
    /// once; unknown ids are rejected. Additive image ids not yet present are ignored (add them via AddImage first).
    /// </summary>
    public void ReorderImages(IReadOnlyList<Guid> orderedFileIds)
    {
        var current = _images.Select(i => i.FileId).ToHashSet();
        var provided = orderedFileIds.ToList();
        if (provided.Count != current.Count || provided.Any(id => !current.Contains(id)) || provided.Distinct().Count() != provided.Count)
            throw new InvalidOperationException("Reorder list must be a permutation of the product's current image file ids.");
        for (var i = 0; i < provided.Count; i++)
            _images.First(img => img.FileId == provided[i]).SetSortOrder(i);
    }

    /// <summary>
    /// Sets (or clears, when null) the thumbnail. A non-null id must be one of the product's gallery images.
    /// </summary>
    public void SetThumbnail(Guid? fileId)
    {
        if (fileId is { } id && id != Guid.Empty && _images.All(i => i.FileId != id))
            throw new InvalidOperationException("Thumbnail must reference one of the product's gallery images.");
        ThumbnailFileId = fileId == Guid.Empty ? null : fileId;
    }
}
