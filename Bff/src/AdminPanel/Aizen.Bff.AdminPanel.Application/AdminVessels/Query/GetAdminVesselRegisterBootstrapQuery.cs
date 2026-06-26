using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel register bootstrap query", "Query to load all options and defaults for the Vessel Register page.")]
public sealed class GetAdminVesselRegisterBootstrapQuery : AizenQuery<AdminVesselRegisterBootstrapBffResponse>
{

    public GetAdminVesselRegisterBootstrapQuery()
    {
    }
}
