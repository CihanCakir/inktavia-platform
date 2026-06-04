using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Resolve dispute command handler", "Resolves a service request dispute via the ServiceRequest module.")]
public sealed class ResolveDisputeCommandHandler
    : AizenCommandHandler<ResolveDisputeCommand, ResolveServiceRequestDisputeResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public ResolveDisputeCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ResolveServiceRequestDisputeResponse?> Handle(
        ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.ResolveDispute(
            request.ServiceRequestId, request.DisputeId, request.Payload, request.Authorization, request.UserToken);
        return result.Body;
    }
}
