using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileWithUserDetailQuery : AizenQuery<OrganizerProfileWithUserDetailDto>
{
    public long ProfileId { get; }

    public GetOrganizerProfileWithUserDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
