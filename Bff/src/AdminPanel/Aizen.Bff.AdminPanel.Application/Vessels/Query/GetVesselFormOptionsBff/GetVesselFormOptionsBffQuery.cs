using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

public sealed class GetVesselFormOptionsBffQuery : AizenQuery<AdminVesselFormOptionsResponse>
{
    public GetVesselFormOptionsBffQuery()
    {
    }
}
