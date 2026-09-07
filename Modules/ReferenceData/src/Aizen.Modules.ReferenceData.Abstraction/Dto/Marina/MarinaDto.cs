namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

/// <summary>A single marina reference record.</summary>
public sealed class MarinaDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    /// <summary>OSM element id (provenance) — surfaced to the admin curation screen. Additive.</summary>
    public string? OsmId { get; set; }
    /// <summary>Uncertain source data flag — drives the admin review queue. Additive.</summary>
    public bool NeedsReview { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Paged admin marina list (curation screen). `{ items, total, page, pageSize }`.</summary>
public sealed class MarinaAdminListResult
{
    public IReadOnlyList<MarinaDto> Items { get; set; } = new List<MarinaDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
