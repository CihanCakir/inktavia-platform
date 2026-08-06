using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Vessel detail BFF DTO", "Full aggregated vessel detail for the Vessel Detail Overview page.")]
public sealed class VesselDetailBffDto
{
    public long Id { get; set; }
    public string VesselCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? VesselTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public string? HeroImageUrl { get; set; }
    public int? YearBuilt { get; set; }
    public string? BuilderName { get; set; }
    public string? BuildCountry { get; set; }
    public string? HullMaterial { get; set; }
    public string? SuperstructureMaterial { get; set; }
    public decimal? BeamMeters { get; set; }
    public decimal? DraftMeters { get; set; }
    public decimal? LengthMeters { get; set; }
    public decimal? GrossTonnage { get; set; }
    public decimal? NetTonnage { get; set; }
    public int? PassengerCapacity { get; set; }
    public int? CrewCapacity { get; set; }
    public string? ImoNumber { get; set; }
    public string? MmsiNumber { get; set; }
    public string? CallSign { get; set; }
    public string? HomePort { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastPositionDate { get; set; }
    public int? OperationalStatus { get; set; }
    public int Status { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? CreateDate { get; set; }
    public VesselEngineBffDto? PrimaryEngine { get; set; }
    public List<CargoDryKitBffDto> CargoDryKits { get; set; } = new();
    public List<ServiceHistoryItemBffDto> ServiceHistory { get; set; } = new();
    public List<DocumentSummaryBffDto> DocumentSummaries { get; set; } = new();
}
