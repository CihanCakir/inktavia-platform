using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Message;

[DocumentationInfo("Vessel media sort order changed message", "Published when vessel media sort order is updated.")]
public sealed class VesselMediaSortOrderChangedMessage : AizenBaseMessage
{
    public long VesselId { get; set; }
    public long MediaId { get; set; }
    public int NewSortOrder { get; set; }
    public long? ActorUserId { get; set; }
}
