using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests — create the owner's service request (optionally publishing it
/// and attaching already-uploaded files), then return the assembled detail.</summary>
public sealed class CreateMobileServiceRequestCommand : AizenCommand<MobileServiceRequestDetailDto>
{
    public CreateMobileServiceRequestCommand(CreateMobileServiceRequestRequest request) => Request = request;

    public CreateMobileServiceRequestRequest Request { get; }
}
