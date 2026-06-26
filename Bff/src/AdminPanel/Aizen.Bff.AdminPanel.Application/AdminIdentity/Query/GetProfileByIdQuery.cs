using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetProfileByIdQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public GetProfileByIdQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
