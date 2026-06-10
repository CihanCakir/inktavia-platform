using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselDocumentsQuery : AizenQuery<AdminVesselDocumentsResponse>
{
    public long VesselId { get; }
    public string UserToken { get; }
    public GetAdminVesselDocumentsQuery(long vesselId, string userToken)
    {
        VesselId = vesselId;
        UserToken = userToken;
    }
}
