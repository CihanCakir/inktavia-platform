using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user detail BFF query", "Fetches full user detail for the admin user detail page.")]
public sealed class GetAdminUserDetailBffQuery : AizenQuery<AdminUserDetailBffResponse>
{
    public long ProfileId { get; }
    public string UserToken { get; }

    public GetAdminUserDetailBffQuery(long profileId, string userToken)
    {
        ProfileId = profileId;
        UserToken = userToken;
    }
}
