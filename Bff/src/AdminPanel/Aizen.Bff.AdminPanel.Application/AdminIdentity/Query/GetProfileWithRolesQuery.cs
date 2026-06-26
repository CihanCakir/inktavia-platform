using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetProfileWithRolesQuery : AizenQuery<ProfileWithRolesResult>
{
    public Guid ProfileId { get; }
    public GetProfileWithRolesQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
