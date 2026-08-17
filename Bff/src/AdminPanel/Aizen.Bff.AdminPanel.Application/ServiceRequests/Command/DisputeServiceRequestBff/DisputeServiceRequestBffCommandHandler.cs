using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Dispute service request admin command handler", "Opens a dispute on a service request via the ServiceRequest module.")]
public sealed class DisputeServiceRequestBffCommandHandler
    : AizenCommandHandler<DisputeServiceRequestBffCommand, DisputeServiceRequestResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public DisputeServiceRequestBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<DisputeServiceRequestResponse?> Handle(
        DisputeServiceRequestBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.DisputeServiceRequest(
            request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
