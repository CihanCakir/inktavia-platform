using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetOrganizerProfileWithUserQuery : AizenQuery<OrganizerProfileWithUserResult>
{
    public Guid ProfileId { get; }
    public string UserToken { get; }
    public GetOrganizerProfileWithUserQuery(Guid profileId, string userToken)
    {
        ProfileId = profileId;
        UserToken = userToken;
    }
}
