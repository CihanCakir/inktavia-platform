using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests — the authenticated participant's own service requests (paged).</summary>
public sealed class GetMobileMyServiceRequestsQuery : AizenQuery<MobileServiceRequestListDto>
{
    public GetMobileMyServiceRequestsQuery(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
    }

    public int PageIndex { get; }
    public int PageSize { get; }
}
