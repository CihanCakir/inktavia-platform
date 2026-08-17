using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user quick BFF query", "Fetches compact user data for the admin panel sidebar quick-view.")]
public sealed class GetUserQuickBffQuery : AizenQuery<AdminUserQuickBffResponse>
{
    public long ProfileId { get; }

    public GetUserQuickBffQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
