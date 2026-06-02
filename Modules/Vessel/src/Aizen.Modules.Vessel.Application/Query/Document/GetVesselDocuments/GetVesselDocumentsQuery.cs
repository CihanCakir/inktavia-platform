using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Query.Document;

public sealed class GetVesselDocumentsQuery : AizenQuery<GetVesselDocumentsResponse>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public bool IncludeAccessUrls { get; }
    public int AccessUrlExpiresInMinutes { get; }

    public GetVesselDocumentsQuery(long vesselId, int pageIndex = 0, int pageSize = 20, bool includeAccessUrls = false, int accessUrlExpiresInMinutes = 15)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
        IncludeAccessUrls = includeAccessUrls;
        AccessUrlExpiresInMinutes = accessUrlExpiresInMinutes;
    }
}
