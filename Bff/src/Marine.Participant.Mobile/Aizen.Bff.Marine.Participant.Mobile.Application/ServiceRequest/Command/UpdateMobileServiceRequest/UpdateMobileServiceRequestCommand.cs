using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>PUT /api/v1/mobile/service-requests/{id} — update one of the caller's own (Draft) service requests.</summary>
public sealed class UpdateMobileServiceRequestCommand : AizenCommand<MobileServiceRequestDetailDto>
{
    public UpdateMobileServiceRequestCommand(long serviceRequestId, UpdateMobileServiceRequestRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public UpdateMobileServiceRequestRequest Request { get; }
}
