using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get service requests by vessel history query", "Returns service history for a specific vessel used by Vessel Detail page and dedicated history endpoint.")]
public sealed class GetServiceRequestsByVesselHistoryQuery : AizenQuery<ServiceRequestVesselHistoryBffResponse>
{
    public long VesselId { get; }
    public int Take { get; }
    public string[]? Statuses { get; }

    public GetServiceRequestsByVesselHistoryQuery(long vesselId, int take = 10, string[]? statuses = null)
    {
        VesselId = vesselId;
        Take = take;
        Statuses = statuses;
    }
}
