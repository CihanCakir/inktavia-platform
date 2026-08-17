using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get service requests by vessel history query", "Returns service history for a specific vessel used by Vessel Detail page and dedicated history endpoint.")]
public sealed class GetServiceRequestsByVesselHistoryBffQuery : AizenQuery<ServiceRequestVesselHistoryBffResponse>
{
    public long VesselId { get; }
    public int Take { get; }
    public string[]? Statuses { get; }

    public GetServiceRequestsByVesselHistoryBffQuery(long vesselId, int take = 10, string[]? statuses = null)
    {
        VesselId = vesselId;
        Take = take;
        Statuses = statuses;
    }
}
