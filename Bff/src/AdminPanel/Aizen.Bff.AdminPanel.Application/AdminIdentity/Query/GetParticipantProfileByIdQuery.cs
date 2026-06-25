using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetParticipantProfileByIdQuery : AizenQuery<ParticipantProfileResult>
{
    public Guid ProfileId { get; }
    public string UserToken { get; }
    public GetParticipantProfileByIdQuery(Guid profileId, string userToken)
    {
        ProfileId = profileId;
        UserToken = userToken;
    }
}
