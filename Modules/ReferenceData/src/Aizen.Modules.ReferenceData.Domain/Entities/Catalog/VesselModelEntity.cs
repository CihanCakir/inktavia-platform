using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

[DocumentationInfo("Vessel model", "A model under a vessel brand, with a production year range (no per-year variants). VesselTypeCode is an existing MARINE lookup.")]
public sealed class VesselModelEntity : AizenEntityWithAudit
{
    public long VesselBrandId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? VesselTypeCode { get; private set; }
    /// <summary>Production start year (null when undocumented — v1 catalog gaps).</summary>
    public int? YearFrom { get; private set; }
    public int? YearTo { get; private set; }
    public decimal? LengthMeters { get; private set; }
    public bool NeedsReview { get; private set; }
    public string? Source { get; private set; }

    public VesselModelEntity() { }

    public static VesselModelEntity Create(
        long vesselBrandId, string code, string name, string? vesselTypeCode,
        int? yearFrom, int? yearTo, decimal? lengthMeters, bool needsReview, string? source)
    {
        if (vesselBrandId <= 0) throw new ArgumentException("VesselBrandId is required.", nameof(vesselBrandId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Model code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Model name is required.", nameof(name));
        if (yearFrom.HasValue && yearTo.HasValue && yearTo.Value < yearFrom.Value) throw new ArgumentException("YearTo cannot be before YearFrom.", nameof(yearTo));
        return new VesselModelEntity
        {
            VesselBrandId = vesselBrandId,
            Code = code.Trim().ToUpperInvariant(),
            Name = CatalogNameNormalizer.Clean(name),
            VesselTypeCode = string.IsNullOrWhiteSpace(vesselTypeCode) ? null : vesselTypeCode.Trim().ToUpperInvariant(),
            YearFrom = yearFrom,
            YearTo = yearTo,
            LengthMeters = lengthMeters,
            NeedsReview = needsReview,
            Source = source?.Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, string? vesselTypeCode, int? yearFrom, int? yearTo, decimal? lengthMeters, bool isActive)
    {
        if (yearFrom.HasValue && yearTo.HasValue && yearTo.Value < yearFrom.Value) throw new ArgumentException("YearTo cannot be before YearFrom.", nameof(yearTo));
        Name = CatalogNameNormalizer.Clean(name);
        VesselTypeCode = string.IsNullOrWhiteSpace(vesselTypeCode) ? null : vesselTypeCode.Trim().ToUpperInvariant();
        YearFrom = yearFrom;
        YearTo = yearTo;
        LengthMeters = lengthMeters;
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
