using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselByIdQuery : AizenQuery<GetVesselDetailResponse>
{
    public long VesselId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminVesselByIdQuery(long vesselId, string authorization, string userToken)
    {
        VesselId = vesselId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
