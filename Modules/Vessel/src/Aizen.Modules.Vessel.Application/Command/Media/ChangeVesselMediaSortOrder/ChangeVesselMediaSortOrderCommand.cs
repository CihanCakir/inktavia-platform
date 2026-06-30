using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Change Vessel Media Sort Order Command", "Carries the payload required to reorder a vessel media item.")]
public sealed class ChangeVesselMediaSortOrderCommand : AizenCommand<ChangeVesselMediaSortOrderResponse>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public int SortOrder { get; }

    public ChangeVesselMediaSortOrderCommand(long vesselId, long mediaId, int sortOrder)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        SortOrder = sortOrder;
    }
}
