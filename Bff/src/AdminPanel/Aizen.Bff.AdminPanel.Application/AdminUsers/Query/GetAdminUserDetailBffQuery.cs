using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user detail BFF query", "Fetches full user detail for the admin user detail page.")]
public sealed class GetAdminUserDetailBffQuery : AizenQuery<AdminUserDetailBffResponse>
{
    public long ProfileId { get; }

    public GetAdminUserDetailBffQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
