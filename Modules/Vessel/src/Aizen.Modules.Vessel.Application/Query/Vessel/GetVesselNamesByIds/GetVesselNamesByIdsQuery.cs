using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Names By Ids Query", "Returns id→name for a given list of vessel IDs — used for bulk enrichment (e.g. the admin service-request list).")]
public sealed class GetVesselNamesByIdsQuery : AizenQuery<List<VesselNameDto>>
{
    public long[] VesselIds { get; }

    public GetVesselNamesByIdsQuery(long[] vesselIds)
    {
        VesselIds = vesselIds;
    }
}
