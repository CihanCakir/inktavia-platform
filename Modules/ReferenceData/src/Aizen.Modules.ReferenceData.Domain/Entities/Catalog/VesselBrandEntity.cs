using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

[DocumentationInfo("Vessel brand", "Boat manufacturer/brand reference for the vessel wizard. NeedsReview flags 'not in list' owner submissions pending admin approval.")]
public sealed class VesselBrandEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? CountryCode { get; private set; }
    public bool NeedsReview { get; private set; }
    /// <summary>Provenance, e.g. "seed", "owner-submission", "admin".</summary>
    public string? Source { get; private set; }

    public VesselBrandEntity() { }

    public static VesselBrandEntity Create(string code, string name, string? countryCode, bool needsReview, string? source)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Brand code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Brand name is required.", nameof(name));
        return new VesselBrandEntity
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = CatalogNameNormalizer.Clean(name),
            CountryCode = countryCode?.Trim().ToUpperInvariant(),
            NeedsReview = needsReview,
            Source = source?.Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, string? countryCode, bool isActive)
    {
        Name = CatalogNameNormalizer.Clean(name);
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        IsActive = isActive;
    }

    public void Approve() => NeedsReview = false;
    public void Activate() => IsActive = true;
    /// <summary>Set when this row was merged into another (audit). Non-null ⇒ deactivated duplicate.</summary>
    public long? MergedIntoId { get; private set; }

    public void Deactivate() => IsActive = false;

    /// <summary>Merge audit: record the surviving target id and deactivate this duplicate.</summary>
    public void MarkMergedInto(long targetId)
    {
        MergedIntoId = targetId;
        IsActive = false;
    }
}
