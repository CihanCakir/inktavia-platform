using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetOrganizerProfileByIdBffQuery : AizenQuery<OrganizerProfileResult>
{
    public Guid ProfileId { get; }
    public GetOrganizerProfileByIdBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
