using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get service requests by vessel history query handler", "Fetches service history items for a vessel. Null-safe: returns empty array with warning if ServiceRequest module is unavailable.")]
public sealed class GetServiceRequestsByVesselHistoryQueryHandler
    : AizenQueryHandler<GetServiceRequestsByVesselHistoryQuery, ServiceRequestVesselHistoryBffResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public GetServiceRequestsByVesselHistoryQueryHandler(
        IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<ServiceRequestVesselHistoryBffResponse?> Handle(
        GetServiceRequestsByVesselHistoryQuery request, CancellationToken cancellationToken)
    {
        var response = new ServiceRequestVesselHistoryBffResponse();

        try
        {

            var result = await _serviceRequest.GetAdminServiceRequestList(
vesselId: request.VesselId, pageIndex: 0, pageSize: request.Take);

            if (result?.Body?.Items != null)
            {
                response.ServiceHistory = result.Body.Items.Select(s => new ServiceHistoryItemBffDto
                {
                    Id = s.Id,
                    Date = s.RequestedStartDate ?? s.CreatedAt,
                    ServiceType = s.ServiceTypeCode ?? s.ServiceCategoryCode,
                    Provider = null, // ProviderName requires Identity/Profile integration — documented as gap
                    Location = s.LocationMarinaName,
                    Notes = s.OwnerNotes ?? s.Title,
                    Status = s.Status.ToString().ToLowerInvariant()
                }).ToList();
            }
        }
        catch
        {
            response.ServiceHistory = Array.Empty<ServiceHistoryItemBffDto>();
            response.Warnings.Add(AdminBffWarning.CallFailed("ServiceRequest.VesselHistory", "Could not retrieve service history for vessel."));
        }

        return response;
    }
}
