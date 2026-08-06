using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetVenueProfileByIdBffQuery : AizenQuery<VenueProfileResult>
{
    public Guid ProfileId { get; }
    public GetVenueProfileByIdBffQuery(Guid profileId)
    {
        ProfileId = profileId;
    }
}
