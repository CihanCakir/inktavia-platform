using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel status history query", "Returns a paged list of status changes for a vessel.")]
public sealed class GetAdminVesselStatusHistoryQuery : AizenQuery<GetVesselStatusHistoryResponse>
{
    public long VesselId { get; }
    public string UserToken { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetAdminVesselStatusHistoryQuery(long vesselId, string userToken, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        UserToken = userToken;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
