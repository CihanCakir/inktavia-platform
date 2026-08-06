using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel register bootstrap query", "Query to load all options and defaults for the Vessel Register page.")]
public sealed class GetVesselRegisterBootstrapBffQuery : AizenQuery<AdminVesselRegisterBootstrapBffResponse>
{

    public GetVesselRegisterBootstrapBffQuery()
    {
    }
}
