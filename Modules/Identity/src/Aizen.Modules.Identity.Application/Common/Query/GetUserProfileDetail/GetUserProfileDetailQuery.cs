using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfileDetailQuery : AizenQuery<UserProfileDetailDto>
{
    public long ProfileId { get; }

    public GetUserProfileDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
