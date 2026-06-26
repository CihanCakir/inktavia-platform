using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetOrganizerProfileByIdQuery : AizenQuery<OrganizerProfileResult>
{
    public Guid ProfileId { get; }
    public GetOrganizerProfileByIdQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
