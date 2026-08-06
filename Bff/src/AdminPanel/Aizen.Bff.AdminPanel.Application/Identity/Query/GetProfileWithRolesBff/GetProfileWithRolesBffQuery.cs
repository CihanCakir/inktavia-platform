using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetProfileWithRolesBffQuery : AizenQuery<ProfileWithRolesResult>
{
    public Guid ProfileId { get; }
    public GetProfileWithRolesBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
