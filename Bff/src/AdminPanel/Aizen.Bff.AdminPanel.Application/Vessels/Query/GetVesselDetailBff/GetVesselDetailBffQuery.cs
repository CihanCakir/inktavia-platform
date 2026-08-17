using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel detail BFF query", "Query for the Vessel Detail Overview page with cross-module aggregation.")]
public sealed class GetVesselDetailBffQuery : AizenQuery<AdminVesselDetailBffResponse>
{
    public long VesselId { get; }

    public GetVesselDetailBffQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
