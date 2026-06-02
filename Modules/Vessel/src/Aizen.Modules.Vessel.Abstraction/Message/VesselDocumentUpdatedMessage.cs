using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel document updated message", "Published when a vessel document's metadata is updated.")]
public sealed class VesselDocumentUpdatedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long DocumentId { get; set; }
    public long? ActorUserId { get; set; }
}
