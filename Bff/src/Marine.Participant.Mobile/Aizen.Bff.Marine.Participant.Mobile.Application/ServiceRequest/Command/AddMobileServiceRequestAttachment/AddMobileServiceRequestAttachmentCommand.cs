using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/attachments — attach a completed client-side upload
/// (fileId) to one of the caller's own service requests.</summary>
public sealed class AddMobileServiceRequestAttachmentCommand : AizenCommand<MobileServiceRequestAttachmentDto>
{
    public AddMobileServiceRequestAttachmentCommand(long serviceRequestId, AddMobileServiceRequestAttachmentRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public AddMobileServiceRequestAttachmentRequest Request { get; }
}
