using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfileDetailQuery : AizenQuery<VenueProfileDetailDto>
{
    public long ProfileId { get; }

    public GetVenueProfileDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
