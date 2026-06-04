using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselDocumentsQuery : AizenQuery<AdminVesselDocumentsResponse>
{
    public long VesselId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminVesselDocumentsQuery(long vesselId, string authorization, string userToken)
    {
        VesselId = vesselId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
