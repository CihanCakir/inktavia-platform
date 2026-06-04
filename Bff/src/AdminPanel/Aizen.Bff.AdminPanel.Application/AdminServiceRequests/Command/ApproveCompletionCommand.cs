using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ApproveCompletionCommand : AizenCommand<ApproveServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public ApproveServiceRequestCompletionRequest Payload { get; }
    public string Authorization { get; }
    public ApproveCompletionCommand(long serviceRequestId, ApproveServiceRequestCompletionRequest payload, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        Authorization = authorization;
    }
}

[DocumentationInfo("Approve completion command handler", "Approves a service request completion via the ServiceRequest module.")]
public sealed class ApproveCompletionCommandHandler
    : AizenCommandHandler<ApproveCompletionCommand, ApproveServiceRequestCompletionResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public ApproveCompletionCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ApproveServiceRequestCompletionResponse?> Handle(
        ApproveCompletionCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.ApproveCompletion(
            request.ServiceRequestId,
            request.Payload,
            request.Authorization);

        return result.Body;
    }
}
