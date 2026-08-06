using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetOrganizerProfileWithUserBffQuery : AizenQuery<OrganizerProfileWithUserResult>
{
    public Guid ProfileId { get; }
    public GetOrganizerProfileWithUserBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
