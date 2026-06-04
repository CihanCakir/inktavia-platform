using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Reject completion command handler", "Rejects a service request completion via the ServiceRequest module.")]
public sealed class RejectCompletionCommandHandler
    : AizenCommandHandler<RejectCompletionCommand, RejectServiceRequestCompletionResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public RejectCompletionCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<RejectServiceRequestCompletionResponse?> Handle(
        RejectCompletionCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.RejectCompletion(request.ServiceRequestId, request.Payload, request.Authorization, request.UserToken);
        return result.Body;
    }
}
