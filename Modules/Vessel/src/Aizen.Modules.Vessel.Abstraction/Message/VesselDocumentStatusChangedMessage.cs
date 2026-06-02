using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel document status changed message", "Published when a vessel document status transitions.")]
public sealed class VesselDocumentStatusChangedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long DocumentId { get; set; }
    public VesselDocumentStatus NewStatus { get; set; }
    public long? ActorUserId { get; set; }
}
