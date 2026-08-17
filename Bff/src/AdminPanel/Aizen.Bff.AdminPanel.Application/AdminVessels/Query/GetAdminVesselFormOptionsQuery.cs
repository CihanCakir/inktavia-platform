using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselFormOptionsQuery : AizenQuery<AdminVesselFormOptionsResponse>
{
    public GetAdminVesselFormOptionsQuery()
    {
    }
}
