using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request dispute opened message", "Published when a dispute is opened.")]
public sealed class ServiceRequestDisputeOpenedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long DisputeId { get; set; }
    public long OpenedByUserId { get; set; }
    public ServiceRequestActorType OpenedByActorType { get; set; }
    public ServiceRequestDisputeReason Reason { get; set; }

    // ── BE-S13c: both parties, so N3 can notify owner + provider (additive; 0 when unknown, e.g. no accepted offer) ──
    public long OwnerUserId { get; set; }
    public long ProviderUserId { get; set; }
}
