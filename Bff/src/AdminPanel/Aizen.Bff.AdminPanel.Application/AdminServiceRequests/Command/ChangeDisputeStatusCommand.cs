using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ChangeDisputeStatusCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ChangeServiceRequestDisputeStatusRequest Payload { get; }
    public string Authorization { get; }
    public ChangeDisputeStatusCommand(long serviceRequestId, long disputeId, ChangeServiceRequestDisputeStatusRequest payload, string authorization)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
        Authorization = authorization;
    }
}

[DocumentationInfo("Change dispute status command handler", "Changes the status of a service request dispute via the ServiceRequest module.")]
public sealed class ChangeDisputeStatusCommandHandler
    : AizenCommandHandler<ChangeDisputeStatusCommand, AdminBffCommandResultDto>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public ChangeDisputeStatusCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ChangeDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.ChangeDisputeStatus(
            request.ServiceRequestId,
            request.DisputeId,
            request.Payload,
            request.Authorization);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Status change failed.");
    }
}
