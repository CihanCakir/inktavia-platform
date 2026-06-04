using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetProfileWithRolesQuery : AizenQuery<ProfileWithRolesResult>
{
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public GetProfileWithRolesQuery(Guid profileId, string authorization, string userToken)
    {
        ProfileId = profileId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
