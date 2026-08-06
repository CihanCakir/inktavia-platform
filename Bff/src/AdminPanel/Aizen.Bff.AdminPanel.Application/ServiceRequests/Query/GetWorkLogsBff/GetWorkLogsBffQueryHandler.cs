using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin work logs query handler", "Fetches work phases, log entries, provider activity and job health for a service request.")]
public sealed class GetWorkLogsBffQueryHandler
    : AizenQueryHandler<GetWorkLogsBffQuery, AdminWorkLogsResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetWorkLogsBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminWorkLogsResponse?> Handle(
        GetWorkLogsBffQuery request, CancellationToken cancellationToken)
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
