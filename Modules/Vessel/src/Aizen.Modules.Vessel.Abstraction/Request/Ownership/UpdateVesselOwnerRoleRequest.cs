using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Ownership;

[DocumentationInfo("Update vessel owner role request", "Input model for changing a vessel owner's role.")]
public sealed class UpdateVesselOwnerRoleRequest
{
    public VesselOwnershipRole Role { get; set; }
}
