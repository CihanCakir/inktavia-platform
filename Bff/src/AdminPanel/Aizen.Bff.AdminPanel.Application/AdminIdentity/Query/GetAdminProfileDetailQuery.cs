using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetAdminProfileDetailQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public string RoleContext { get; }
    public GetAdminProfileDetailQuery(Guid profileId, string roleContext)
    {
        ProfileId = profileId;
        RoleContext = roleContext;
    }
}
