using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Vessel;

[DocumentationInfo("Archive vessel request", "Input model for archiving a vessel.")]
public sealed class ArchiveVesselRequest
{
    public VesselArchiveReason Reason { get; set; }
    public string? Notes { get; set; }
}
