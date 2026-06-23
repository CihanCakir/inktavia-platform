using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user activity BFF query", "Returns the activity timeline for a specific user.")]
public sealed class GetAdminUserActivityBffQuery : AizenQuery<AdminUserActivityBffResponse>
{
    public long ProfileId { get; }
    public string UserToken { get; }

    public GetAdminUserActivityBffQuery(long profileId, string userToken)
    {
        ProfileId = profileId;
        UserToken = userToken;
    }
}
