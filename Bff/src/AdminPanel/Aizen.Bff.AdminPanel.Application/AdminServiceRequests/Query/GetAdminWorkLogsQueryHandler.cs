using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin work logs query handler", "Fetches work phases, log entries, provider activity and job health for a service request.")]
public sealed class GetAdminWorkLogsQueryHandler
    : AizenQueryHandler<GetAdminWorkLogsQuery, AdminWorkLogsResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetAdminWorkLogsQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminWorkLogsResponse?> Handle(
        GetAdminWorkLogsQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminWorkLogsResponse();

        try
        {

            var result = await _serviceRequest.GetAdminWorkLogs(
                request.ServiceRequestId);
            response.WorkLogs = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
