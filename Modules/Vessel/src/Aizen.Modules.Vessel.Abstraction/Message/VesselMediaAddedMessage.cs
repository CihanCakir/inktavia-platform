using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel media added message", "Published when a media file is attached to a vessel.")]
public sealed class VesselMediaAddedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long MediaId { get; set; }
    public Guid? FileId { get; set; }
    public bool IsCover { get; set; }
    public long? ActorUserId { get; set; }
}
