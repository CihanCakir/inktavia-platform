using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Add work log entry admin command handler", "Adds a new work log entry to a service request via the ServiceRequest module.")]
public sealed class AddWorkLogEntryBffCommandHandler
    : AizenCommandHandler<AddWorkLogEntryBffCommand, AddWorkLogEntryResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public AddWorkLogEntryBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AddWorkLogEntryResponse?> Handle(
        AddWorkLogEntryBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.AddAdminWorkLogEntry(
            request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
