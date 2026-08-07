using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get maintenance schedule list query handler",
    "Fetches active recurring maintenance schedules (S12), optionally scoped to a vessel.")]
public sealed class GetMaintenanceScheduleListBffQueryHandler
    : AizenQueryHandler<GetMaintenanceScheduleListBffQuery, GetMaintenanceScheduleListResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetMaintenanceScheduleListBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<GetMaintenanceScheduleListResponse?> Handle(
        GetMaintenanceScheduleListBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.GetAdminMaintenanceScheduleList(request.VesselId, request.IncludeInactive);
        return result.Body;
    }
}
