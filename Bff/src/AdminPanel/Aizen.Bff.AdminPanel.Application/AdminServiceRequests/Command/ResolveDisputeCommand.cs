using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ResolveDisputeCommand : AizenCommand<ResolveServiceRequestDisputeResponse>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ResolveServiceRequestDisputeRequest Payload { get; }
    public string Authorization { get; }
    public ResolveDisputeCommand(long serviceRequestId, long disputeId, ResolveServiceRequestDisputeRequest payload, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
        Authorization = authorization;
    }
}

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
            request.ServiceRequestId,
            request.DisputeId,
            request.Payload,
            request.Authorization);

        return result.Body;
    }
}
