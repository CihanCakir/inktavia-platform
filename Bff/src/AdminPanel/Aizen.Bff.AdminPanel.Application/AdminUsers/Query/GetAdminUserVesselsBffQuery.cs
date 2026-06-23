using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user vessels BFF query", "Returns the vessel list for a specific user's vessels tab.")]
public sealed class GetAdminUserVesselsBffQuery : AizenQuery<AdminUserVesselsBffResponse>
{
    public long ProfileId { get; }
    public string UserToken { get; }

    public GetAdminUserVesselsBffQuery(long profileId, string userToken)
    {
        ProfileId = profileId;
        UserToken = userToken;
    }
}
