using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel document removed message", "Published when a vessel document is deactivated.")]
public sealed class VesselDocumentRemovedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long DocumentId { get; set; }
    public long? ActorUserId { get; set; }
}
