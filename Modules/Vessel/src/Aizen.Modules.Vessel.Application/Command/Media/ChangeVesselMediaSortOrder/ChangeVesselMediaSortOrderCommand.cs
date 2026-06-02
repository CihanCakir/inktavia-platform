using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Change Vessel Media Sort Order Command", "Carries the payload required to reorder a vessel media item.")]
public sealed class ChangeVesselMediaSortOrderCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public int SortOrder { get; }
    public long RequestingUserId { get; }

    public ChangeVesselMediaSortOrderCommand(long vesselId, long mediaId, int sortOrder, long requestingUserId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        SortOrder = sortOrder;
        RequestingUserId = requestingUserId;
    }
}
