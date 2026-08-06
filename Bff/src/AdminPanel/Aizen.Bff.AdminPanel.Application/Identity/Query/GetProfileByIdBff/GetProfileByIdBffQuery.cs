using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetProfileByIdBffQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public GetProfileByIdBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
