using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Ownership;

[DocumentationInfo("Add vessel owner request", "Input model for inviting a user as a vessel owner/role.")]
public sealed class AddVesselOwnerRequest
{
    public long UserId { get; set; }
    public VesselOwnershipRole Role { get; set; }
}
