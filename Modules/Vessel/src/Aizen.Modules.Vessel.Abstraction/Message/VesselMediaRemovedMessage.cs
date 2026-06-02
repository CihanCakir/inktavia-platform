using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel media removed message", "Published when a vessel media item is deactivated.")]
public sealed class VesselMediaRemovedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long MediaId { get; set; }
    public long? ActorUserId { get; set; }
}
