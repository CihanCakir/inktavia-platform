
namespace Aizen.Modules.Vessel.Abstraction.Request.Specification;

[DocumentationInfo("Upsert vessel specification request", "Creates or updates physical/technical measurements for a vessel.")]
public sealed class UpsertVesselSpecificationRequest
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    /// <summary>Optional ReferenceData catalog link. Brand/Model above are the denormalized display fallback.</summary>
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
}
