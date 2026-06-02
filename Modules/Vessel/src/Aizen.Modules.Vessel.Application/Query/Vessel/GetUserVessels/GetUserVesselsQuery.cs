using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetUserVesselsQuery : AizenPagedQuery<VesselListItemDto>
{
    public long UserId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetUserVesselsQuery(long userId, int pageIndex = 0, int pageSize = 20)
    {
        UserId = userId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
