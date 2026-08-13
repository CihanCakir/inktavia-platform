using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Status Counts Query", "Returns per-status vessel counts over the whole (non-deleted) fleet — the admin dashboard fleet-status chart (C3).")]
public sealed class GetVesselStatusCountsQuery : AizenQuery<List<VesselStatusCountDto>>
{
}
