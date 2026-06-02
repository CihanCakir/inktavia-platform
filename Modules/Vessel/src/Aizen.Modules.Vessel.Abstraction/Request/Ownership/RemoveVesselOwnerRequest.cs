using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Ownership;

[DocumentationInfo("Remove vessel owner request", "Input model for removing an owner from a vessel.")]
public sealed class RemoveVesselOwnerRequest
{
    public string? Reason { get; set; }
}
