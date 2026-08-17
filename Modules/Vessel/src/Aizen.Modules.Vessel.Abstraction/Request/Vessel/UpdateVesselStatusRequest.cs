using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Vessel;

[DocumentationInfo("Update vessel status request", "Input model for changing a vessel's operational status.")]
public sealed class UpdateVesselStatusRequest
{
    public VesselStatus Status { get; set; }
    public string? Reason { get; set; }
}
