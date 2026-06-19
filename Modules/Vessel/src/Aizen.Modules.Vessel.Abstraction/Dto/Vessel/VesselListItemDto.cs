using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

[DocumentationInfo("Vessel list item DTO", "Compact vessel representation for list queries.")]
public sealed class VesselListItemDto
{
    public long Id { get; set; }
    public Guid? PublicId { get; set; }
    public string VesselCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string VesselTypeCode { get; set; } = default!;
    public string? FlagCountryCode { get; set; }
    public string? CoverMediaUrl { get; set; }
    public VesselStatus Status { get; set; }
    public VesselVisibility Visibility { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? CreateDate { get; set; }
    public int? OperationalStatus { get; set; }
    public int? AssetType { get; set; }
    public int? OwnershipStatus { get; set; }
    public string? OwnerName { get; set; }
    public decimal? LengthMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastPositionDate { get; set; }
}
