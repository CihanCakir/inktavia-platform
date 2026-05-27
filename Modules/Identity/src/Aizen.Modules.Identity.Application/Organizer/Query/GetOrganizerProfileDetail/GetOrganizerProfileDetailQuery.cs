using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileDetailQuery : AizenQuery<OrganizerProfileDetailDto>
{
    public long ProfileId { get; }

    public GetOrganizerProfileDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
