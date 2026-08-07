using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/cancel — cancel one of the caller's own service requests
/// with the N-E structured reason + optional note. Returns the re-read detail (status = Cancelled).</summary>
public sealed class CancelMobileServiceRequestCommand : AizenCommand<MobileServiceRequestDetailDto>
{
    public CancelMobileServiceRequestCommand(long serviceRequestId, CancelMobileServiceRequestRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public CancelMobileServiceRequestRequest Request { get; }
}
