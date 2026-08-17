using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

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

    // Owner display — enriched via Identity bulk lookup.
    public long? OwnerUserId { get; set; }
    public long? OwnerProfileId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerAvatarUrl { get; set; }

    public decimal? LengthMeters { get; set; }
    public decimal? GrossTonnage { get; set; }

    // Latest location snapshot.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastPositionDate { get; set; }
    public string? LastLocationText { get; set; }

    // Numeric status values — preserved for filters.
    public int? OperationalStatus { get; set; }
    public int? AssetType { get; set; }
    public int? OwnershipStatus { get; set; }
    public int Status { get; set; }

    // UI label strings — mapped from numeric enums in BFF.
    public string? OperationalStatusLabel { get; set; }
    public string? AssetTypeLabel { get; set; }
    public string? OwnershipStatusLabel { get; set; }
    public string? StatusLabel { get; set; }

    public bool IsArchived { get; set; }
    public DateTime? CreateDate { get; set; }
}
