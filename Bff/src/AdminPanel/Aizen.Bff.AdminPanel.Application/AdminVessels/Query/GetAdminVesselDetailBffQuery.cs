using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel detail BFF query", "Query for the Vessel Detail Overview page with cross-module aggregation.")]
public sealed class GetAdminVesselDetailBffQuery : AizenQuery<AdminVesselDetailBffResponse>
{
    public long VesselId { get; }
    public string UserToken { get; }

    public GetAdminVesselDetailBffQuery(long vesselId, string userToken)
    {
        VesselId = vesselId;
        UserToken = userToken;
    }
}
