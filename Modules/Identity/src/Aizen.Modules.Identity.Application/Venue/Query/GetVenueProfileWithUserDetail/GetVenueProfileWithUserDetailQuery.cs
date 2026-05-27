using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfileWithUserDetailQuery : AizenQuery<VenueProfileWithUserDetailDto>
{
    public long ProfileId { get; }

    public GetVenueProfileWithUserDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
