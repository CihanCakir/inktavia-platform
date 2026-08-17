
namespace Aizen.Modules.Vessel.Abstraction.Request.Engine;

[DocumentationInfo("Update vessel engine request", "Input model for updating an existing vessel engine.")]
public sealed class UpdateVesselEngineRequest
{
    public string EngineName { get; set; } = default!;
    public string EngineTypeCode { get; set; } = default!;
    public string FuelTypeCode { get; set; } = default!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? HorsePower { get; set; }
    public int? ProductionYear { get; set; }
}
