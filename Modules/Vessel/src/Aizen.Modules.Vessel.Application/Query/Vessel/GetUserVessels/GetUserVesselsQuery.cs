using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetUserVesselsQuery : AizenQuery<IReadOnlyList<VesselListItemDto>>
{
    public long UserId { get; }

    public GetUserVesselsQuery(long userId)
    {
        UserId = userId;
    }
}
