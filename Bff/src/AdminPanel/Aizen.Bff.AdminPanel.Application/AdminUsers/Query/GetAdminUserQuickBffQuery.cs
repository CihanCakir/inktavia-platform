using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user quick BFF query", "Fetches compact user data for the admin panel sidebar quick-view.")]
public sealed class GetAdminUserQuickBffQuery : AizenQuery<AdminUserQuickBffResponse>
{
    public long ProfileId { get; }

    public GetAdminUserQuickBffQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
