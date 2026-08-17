
namespace Aizen.Modules.Vessel.Abstraction.Request.Engine;

[DocumentationInfo("Add vessel engine request", "Input model for adding a new engine to a vessel.")]
public sealed class AddVesselEngineRequest
{
    public string EngineName { get; set; } = default!;
    public string EngineTypeCode { get; set; } = default!;
    public string FuelTypeCode { get; set; } = default!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? HorsePower { get; set; }
    public int? ProductionYear { get; set; }
    public bool IsPrimary { get; set; }
}
