using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Marina;

[DocumentationInfo(
    "Marina reference entity",
    "Catalog of marinas and fishing harbours (balıkçı barınağı) with coordinates + province, sourced from OpenStreetMap (ODbL). Backs the nearest-marina query. Free-text marina names on vessels are unaffected.")]
public sealed class MarinaEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    /// <summary>"MARINA" | "FISHING_HARBOR".</summary>
    public string Type { get; private set; } = default!;
    public string? CountryCode { get; private set; }
    /// <summary>il plate code (e.g. "34") linking to the Location tree, when the province is seeded there.</summary>
    public string? CityCode { get; private set; }
    /// <summary>Denormalized il name for display (e.g. "İstanbul").</summary>
    public string? Province { get; private set; }
    /// <summary>ilçe, when derivable.</summary>
    public string? District { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    /// <summary>OSM element id (e.g. "n823443291") for traceability + idempotent re-seeding.</summary>
    public string? OsmId { get; private set; }
    /// <summary>Flagged where the source data is uncertain (inferred province, synthesized name).</summary>
    public bool NeedsReview { get; private set; }

    public const string TypeMarina = "MARINA";
    public const string TypeFishingHarbor = "FISHING_HARBOR";

    public MarinaEntity() { }

    public static MarinaEntity Create(
        string code, string name, string type, string? countryCode, string? cityCode,
        string? province, string? district, decimal latitude, decimal longitude,
        string? osmId, bool needsReview)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Marina code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Marina name is required.", nameof(name));
        if (latitude < -90m || latitude > 90m) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180m || longitude > 180m) throw new ArgumentOutOfRangeException(nameof(longitude));

        return new MarinaEntity
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Type = NormalizeType(type),
            CountryCode = countryCode?.Trim().ToUpperInvariant(),
            CityCode = cityCode?.Trim().ToUpperInvariant(),
            Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim(),
            District = string.IsNullOrWhiteSpace(district) ? null : district.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            OsmId = string.IsNullOrWhiteSpace(osmId) ? null : osmId.Trim(),
            NeedsReview = needsReview,
            IsActive = true
        };
    }

    public void Update(
        string name, string type, string? countryCode, string? cityCode, string? province, string? district,
        decimal latitude, decimal longitude, string? osmId, bool needsReview, bool isActive)
    {
        if (latitude < -90m || latitude > 90m) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180m || longitude > 180m) throw new ArgumentOutOfRangeException(nameof(longitude));
        Name = name.Trim();
        Type = NormalizeType(type);
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        CityCode = cityCode?.Trim().ToUpperInvariant();
        Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim();
        District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
        Latitude = latitude;
        Longitude = longitude;
        OsmId = string.IsNullOrWhiteSpace(osmId) ? null : osmId.Trim();
        NeedsReview = needsReview;
        IsActive = isActive;
    }

    private static string NormalizeType(string type)
    {
        var t = (type ?? string.Empty).Trim().ToUpperInvariant();
        return t == TypeFishingHarbor ? TypeFishingHarbor : TypeMarina;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
