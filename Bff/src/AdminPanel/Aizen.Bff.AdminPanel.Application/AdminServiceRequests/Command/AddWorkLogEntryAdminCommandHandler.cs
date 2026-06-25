using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Add work log entry admin command handler", "Adds a new work log entry to a service request via the ServiceRequest module.")]
public sealed class AddWorkLogEntryAdminCommandHandler
    : AizenCommandHandler<AddWorkLogEntryAdminCommand, AddWorkLogEntryResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public AddWorkLogEntryAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AddWorkLogEntryResponse?> Handle(
        AddWorkLogEntryAdminCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _serviceRequest.AddAdminWorkLogEntry(
            request.ServiceRequestId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
