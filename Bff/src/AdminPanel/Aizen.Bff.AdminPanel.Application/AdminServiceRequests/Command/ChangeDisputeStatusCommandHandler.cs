using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Change dispute status command handler", "Changes the status of a service request dispute via the ServiceRequest module.")]
public sealed class ChangeDisputeStatusCommandHandler
    : AizenCommandHandler<ChangeDisputeStatusCommand, AdminBffCommandResultDto>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public ChangeDisputeStatusCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ChangeDisputeStatusCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.ChangeDisputeStatus(
            request.ServiceRequestId, request.DisputeId, request.Payload);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Status change failed.");
    }
}
