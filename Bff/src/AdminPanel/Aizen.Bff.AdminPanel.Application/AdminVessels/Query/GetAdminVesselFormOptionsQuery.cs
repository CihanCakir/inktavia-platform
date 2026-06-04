using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselFormOptionsQuery : AizenQuery<AdminVesselFormOptionsResponse>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminVesselFormOptionsQuery(string authorization, string userToken)
    {
        Authorization = authorization;
        UserToken = userToken;
    }
}
