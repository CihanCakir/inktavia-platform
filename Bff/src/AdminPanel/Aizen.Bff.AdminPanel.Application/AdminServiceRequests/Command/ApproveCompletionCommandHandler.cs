using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Approve completion command handler", "Approves a service request completion via the ServiceRequest module.")]
public sealed class ApproveCompletionCommandHandler
    : AizenCommandHandler<ApproveCompletionCommand, ApproveServiceRequestCompletionResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public ApproveCompletionCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ApproveServiceRequestCompletionResponse?> Handle(
        ApproveCompletionCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.ApproveCompletion(request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
