using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Status;

[DocumentationInfo("Get vessel status history by owner response", "Flat list of status change events for all vessels owned by a given user.")]
public sealed class GetVesselStatusHistoryByOwnerResponse
{
    public List<VesselStatusHistoryByOwnerItemDto> Items { get; init; } = new();
}
