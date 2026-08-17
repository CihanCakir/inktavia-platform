using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Reject completion command handler", "Rejects a service request completion via the ServiceRequest module.")]
public sealed class RejectCompletionBffCommandHandler
    : AizenCommandHandler<RejectCompletionBffCommand, RejectServiceRequestCompletionResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public RejectCompletionBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<RejectServiceRequestCompletionResponse?> Handle(
        RejectCompletionBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.RejectCompletion(request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
