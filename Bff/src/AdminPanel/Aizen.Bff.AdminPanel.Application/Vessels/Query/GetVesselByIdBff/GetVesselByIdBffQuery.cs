using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

public sealed class GetVesselByIdBffQuery : AizenQuery<AdminVesselByIdBffResponse>
{
    public long VesselId { get; }
    public GetVesselByIdBffQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
