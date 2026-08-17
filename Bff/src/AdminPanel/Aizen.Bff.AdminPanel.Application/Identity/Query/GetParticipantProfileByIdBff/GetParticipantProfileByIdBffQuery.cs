using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetParticipantProfileByIdBffQuery : AizenQuery<ParticipantProfileResult>
{
    public Guid ProfileId { get; }
    public GetParticipantProfileByIdBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
