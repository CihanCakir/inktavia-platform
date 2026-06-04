using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class CancelServiceRequestCommand : AizenCommand<CancelServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CancelServiceRequestRequest Payload { get; }
    public string Authorization { get; }
    public CancelServiceRequestCommand(long serviceRequestId, CancelServiceRequestRequest payload, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        Authorization = authorization;
    }
}

[DocumentationInfo("Cancel service request command handler", "Cancels a service request via the ServiceRequest module.")]
public sealed class CancelServiceRequestCommandHandler
    : AizenCommandHandler<CancelServiceRequestCommand, CancelServiceRequestResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public CancelServiceRequestCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<CancelServiceRequestResponse?> Handle(
        CancelServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.CancelServiceRequest(
            request.ServiceRequestId,
            request.Payload,
            request.Authorization);

        return result.Body;
    }
}
