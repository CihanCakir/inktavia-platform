using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/completion/approve — the owner approves the provider's
/// completion (optional 1..5 rating + note) → SR Completed → the existing decoupled escrow release runs (untouched).
/// Returns the re-read completion (status = ApprovedByOwner). BFF owner-gated; no amounts are client-supplied.</summary>
public sealed class ApproveMobileServiceRequestCompletionCommand : AizenCommand<MobileServiceRequestCompletionDto>
{
    public ApproveMobileServiceRequestCompletionCommand(long serviceRequestId, ApproveMobileCompletionRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public ApproveMobileCompletionRequest Request { get; }
}
