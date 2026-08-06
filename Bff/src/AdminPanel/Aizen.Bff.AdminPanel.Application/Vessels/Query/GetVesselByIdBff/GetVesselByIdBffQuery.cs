using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

public sealed class GetVesselByIdBffQuery : AizenQuery<GetVesselDetailResponse>
{
    public long VesselId { get; }
    public GetVesselByIdBffQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
