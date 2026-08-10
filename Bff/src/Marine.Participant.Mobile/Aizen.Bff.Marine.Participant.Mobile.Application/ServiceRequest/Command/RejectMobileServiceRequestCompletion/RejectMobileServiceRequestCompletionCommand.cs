using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/completion/reject — the owner rejects the provider's completion
/// with the N-E structured reason + optional note (SR → InProgress; no money). Returns the re-read completion
/// (status = RejectedByOwner). BFF owner-gated.</summary>
public sealed class RejectMobileServiceRequestCompletionCommand : AizenCommand<MobileServiceRequestCompletionDto>
{
    public RejectMobileServiceRequestCompletionCommand(long serviceRequestId, RejectMobileCompletionRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public RejectMobileCompletionRequest Request { get; }
}
