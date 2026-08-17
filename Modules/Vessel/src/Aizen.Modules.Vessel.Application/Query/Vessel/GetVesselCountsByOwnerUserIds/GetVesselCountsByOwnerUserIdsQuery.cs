using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Counts By Owner User Ids Query", "Returns vessel count per userId for a given list of user IDs — used for bulk enrichment in admin user list.")]
public sealed class GetVesselCountsByOwnerUserIdsQuery : AizenQuery<List<VesselCountByOwnerDto>>
{
    public long[] UserIds { get; }

    public GetVesselCountsByOwnerUserIdsQuery(long[] userIds)
    {
        UserIds = userIds;
    }
}
