using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselDocumentsQuery : AizenQuery<AdminVesselDocumentsResponse>
{
    public long VesselId { get; }
    public GetAdminVesselDocumentsQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
