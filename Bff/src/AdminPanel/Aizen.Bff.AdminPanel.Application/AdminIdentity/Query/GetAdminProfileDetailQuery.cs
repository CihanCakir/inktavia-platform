using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetAdminProfileDetailQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public string RoleContext { get; }
    public GetAdminProfileDetailQuery(Guid profileId, string authorization, string userToken, string roleContext)
    {
        ProfileId = profileId;
        Authorization = authorization;
        UserToken = userToken;
        RoleContext = roleContext;
    }
}
