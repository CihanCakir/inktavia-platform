using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/dispute — the owner opens a dispute (N-E structured reason +
/// description) on one of their own SRs. BFF owner-gated (the shared module open endpoint does not owner-check);
/// the actor resolves to Owner module-side. Returns the just-opened dispute as a list row (for immediate navigation).
/// Cost-free. Resolution / status-change are NOT exposed here (Admin-only).</summary>
public sealed class OpenMobileDisputeCommand : AizenCommand<MobileDisputeListItemDto>
{
    public OpenMobileDisputeCommand(long serviceRequestId, OpenMobileDisputeRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public OpenMobileDisputeRequest Request { get; }
}
