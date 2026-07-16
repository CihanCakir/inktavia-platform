using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel.GetVesselSummaries;

[DocumentationInfo("Get vessel summaries query", "Returns summary DTOs for a batch of vessel ids. Ids that don't resolve are omitted.")]
public sealed class GetVesselSummariesQuery : AizenQuery<GetVesselSummariesResponse>
{
    public long[] Ids { get; }
    public GetVesselSummariesQuery(long[] ids) => Ids = ids;
}
