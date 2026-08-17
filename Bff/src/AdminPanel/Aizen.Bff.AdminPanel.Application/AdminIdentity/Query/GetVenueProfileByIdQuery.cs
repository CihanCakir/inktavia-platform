using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetVenueProfileByIdQuery : AizenQuery<VenueProfileResult>
{
    public Guid ProfileId { get; }
    public GetVenueProfileByIdQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
