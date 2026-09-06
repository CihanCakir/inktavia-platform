
namespace Aizen.Modules.Vessel.Abstraction.Dto.Specification;

[DocumentationInfo("Vessel specification DTO", "Physical and technical measurements of a vessel.")]
public sealed class VesselSpecificationDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public long? VesselBrandId { get; set; }
    public long? VesselModelId { get; set; }
    public int? ProductionYear { get; set; }
    public decimal? LengthValue { get; set; }
    public string? LengthUnitCode { get; set; }
    public decimal? BeamValue { get; set; }
    public string? BeamUnitCode { get; set; }
    public decimal? DraftValue { get; set; }
    public string? DraftUnitCode { get; set; }
    public decimal? WeightValue { get; set; }
    public string? WeightUnitCode { get; set; }
    public int? CabinCount { get; set; }
    public int? BedCount { get; set; }
    public int? BathroomCount { get; set; }
    public string? HullMaterialCode { get; set; }
    public decimal? FuelCapacityValue { get; set; }
    public string? FuelCapacityUnitCode { get; set; }
    public decimal? WaterCapacityValue { get; set; }
    public string? WaterCapacityUnitCode { get; set; }
    public string? BuildCountry { get; set; }
    public string? SuperstructureMaterial { get; set; }
    public decimal? GrossTonnage { get; set; }
    public decimal? NetTonnage { get; set; }
    public int? PassengerCapacity { get; set; }
    public int? CrewCapacity { get; set; }
}
