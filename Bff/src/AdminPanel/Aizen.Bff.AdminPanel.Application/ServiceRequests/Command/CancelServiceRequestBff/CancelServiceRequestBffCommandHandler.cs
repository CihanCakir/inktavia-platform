using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Cancel service request command handler", "Cancels a service request via the ServiceRequest module.")]
public sealed class CancelServiceRequestBffCommandHandler
    : AizenCommandHandler<CancelServiceRequestBffCommand, CancelServiceRequestResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public CancelServiceRequestBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<CancelServiceRequestResponse?> Handle(
        CancelServiceRequestBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.CancelServiceRequest(request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
