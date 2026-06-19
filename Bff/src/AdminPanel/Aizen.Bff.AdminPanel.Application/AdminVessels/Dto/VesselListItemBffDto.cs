using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Vessel list item BFF DTO", "Flat, UI-ready vessel item for the Vessel Management List page.")]
public sealed class VesselListItemBffDto
{
    public long Id { get; set; }
    public string VesselCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? OwnerName { get; set; }
    public decimal? LengthMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastPositionDate { get; set; }
    public int? OperationalStatus { get; set; }
    public int? AssetType { get; set; }
    public int? OwnershipStatus { get; set; }
    public int Status { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? CreateDate { get; set; }
}
