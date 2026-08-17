namespace Aizen.Modules.ReferenceData.Domain.Documents.Location;

/// <summary>M3 — a location document that carries a URL slug (country/city/district/neighborhood).</summary>
public interface ISluggableLocation
{
    string? Slug { get; set; }
}
