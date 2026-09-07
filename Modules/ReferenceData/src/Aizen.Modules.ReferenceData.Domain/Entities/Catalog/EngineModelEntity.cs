using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

[DocumentationInfo("Engine model", "A model under an engine brand. FuelTypeCode + EngineTypeCode are existing MARINE lookups.")]
public sealed class EngineModelEntity : AizenEntityWithAudit
{
    public long EngineBrandId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int? HorsePower { get; private set; }
    public string? FuelTypeCode { get; private set; }
    public string? EngineTypeCode { get; private set; }
    public int? YearFrom { get; private set; }
    public int? YearTo { get; private set; }
    public bool NeedsReview { get; private set; }
    public string? Source { get; private set; }

    public EngineModelEntity() { }

    public static EngineModelEntity Create(
        long engineBrandId, string code, string name, int? horsePower, string? fuelTypeCode, string? engineTypeCode,
        int? yearFrom, int? yearTo, bool needsReview, string? source)
    {
        if (engineBrandId <= 0) throw new ArgumentException("EngineBrandId is required.", nameof(engineBrandId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Model code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Model name is required.", nameof(name));
        if (yearFrom.HasValue && yearTo.HasValue && yearTo.Value < yearFrom.Value) throw new ArgumentException("YearTo cannot be before YearFrom.", nameof(yearTo));
        return new EngineModelEntity
        {
            EngineBrandId = engineBrandId,
            Code = code.Trim().ToUpperInvariant(),
            Name = CatalogNameNormalizer.Clean(name),
            HorsePower = horsePower,
            FuelTypeCode = string.IsNullOrWhiteSpace(fuelTypeCode) ? null : fuelTypeCode.Trim().ToUpperInvariant(),
            EngineTypeCode = string.IsNullOrWhiteSpace(engineTypeCode) ? null : engineTypeCode.Trim().ToUpperInvariant(),
            YearFrom = yearFrom,
            YearTo = yearTo,
            NeedsReview = needsReview,
            Source = source?.Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, int? horsePower, string? fuelTypeCode, string? engineTypeCode, int? yearFrom, int? yearTo, bool isActive)
    {
        if (yearFrom.HasValue && yearTo.HasValue && yearTo.Value < yearFrom.Value) throw new ArgumentException("YearTo cannot be before YearFrom.", nameof(yearTo));
        Name = CatalogNameNormalizer.Clean(name);
        HorsePower = horsePower;
        FuelTypeCode = string.IsNullOrWhiteSpace(fuelTypeCode) ? null : fuelTypeCode.Trim().ToUpperInvariant();
        EngineTypeCode = string.IsNullOrWhiteSpace(engineTypeCode) ? null : engineTypeCode.Trim().ToUpperInvariant();
        YearFrom = yearFrom;
        YearTo = yearTo;
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
