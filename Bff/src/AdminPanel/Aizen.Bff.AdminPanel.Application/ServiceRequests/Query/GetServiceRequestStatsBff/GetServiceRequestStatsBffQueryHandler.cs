using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request stats query handler",
    "Returns the SR list KPIs (real totalRequests/activeRepairs/criticalAlerts from the module). repairVelocity/recentAlerts are deferred (empty) → the FE keeps its localized empty-state.")]
public sealed class GetServiceRequestStatsBffQueryHandler
    : AizenQueryHandler<GetServiceRequestStatsBffQuery, ServiceRequestStatsBffDto>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetServiceRequestStatsBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ServiceRequestStatsBffDto?> Handle(
        GetServiceRequestStatsBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.GetAdminServiceRequestStats();
        var body = result.Body;

        return new ServiceRequestStatsBffDto
        {
            TotalRequests = body?.TotalRequests ?? 0,
            ActiveRepairs = body?.ActiveRepairs ?? 0,
            CriticalAlerts = body?.CriticalAlerts ?? 0,
            // repairVelocity / recentAlerts deferred (empty) — FE renders its localized empty-state.
        };
    }
}
