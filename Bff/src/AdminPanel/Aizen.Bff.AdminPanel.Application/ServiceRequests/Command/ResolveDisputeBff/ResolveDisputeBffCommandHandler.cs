using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Resolve dispute command handler", "Resolves a service request dispute via the ServiceRequest module.")]
public sealed class ResolveDisputeBffCommandHandler
    : AizenCommandHandler<ResolveDisputeBffCommand, ResolveServiceRequestDisputeResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public ResolveDisputeBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ResolveServiceRequestDisputeResponse?> Handle(
        ResolveDisputeBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.ResolveDispute(
            request.ServiceRequestId, request.DisputeId, request.Payload);
        return result.Body;
    }
}
