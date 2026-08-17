using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel document added message", "Published when a document is attached to a vessel.")]
public sealed class VesselDocumentAddedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long DocumentId { get; set; }
    public string DocumentTypeCode { get; set; } = default!;
    public Guid? FileId { get; set; }
    public long? ActorUserId { get; set; }
}
