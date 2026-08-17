using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Create offer command", "Provider creates an offer for a service request.")]
public sealed class CreateServiceRequestOfferCommand : AizenCommand<CreateServiceRequestOfferResponse>
{
    public long ServiceRequestId { get; }
    public CreateServiceRequestOfferRequest Request { get; }
    public CreateServiceRequestOfferCommand(long serviceRequestId, CreateServiceRequestOfferRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
