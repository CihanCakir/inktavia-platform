using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel cover media changed message", "Published when the cover media for a vessel changes.")]
public sealed class VesselCoverMediaChangedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long CoverMediaId { get; set; }
    public long? ActorUserId { get; set; }
}
