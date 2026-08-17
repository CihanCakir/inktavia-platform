using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetProfileDetailBffQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public string RoleContext { get; }
    public GetProfileDetailBffQuery(Guid profileId, string roleContext)
    {
        ProfileId = profileId;
        RoleContext = roleContext;
    }
}
