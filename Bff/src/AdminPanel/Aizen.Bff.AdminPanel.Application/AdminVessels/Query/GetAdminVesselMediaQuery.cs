using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media query", "Returns a paged list of media files for a vessel.")]
public sealed class GetAdminVesselMediaQuery : AizenQuery<GetVesselMediaResponse>
{
    public long VesselId { get; }
    public string UserToken { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetAdminVesselMediaQuery(long vesselId, string userToken, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        UserToken = userToken;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
