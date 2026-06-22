using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

/// <summary>Admin bulk lookup of user profiles by a list of user IDs. Used by BFF enrichment flows.</summary>
public sealed class GetUserProfilesByUserIdsQuery : AizenListedQuery<UserProfileListItemDto>
{
    public long[] UserIds { get; }

    public GetUserProfilesByUserIdsQuery(long[] userIds)
    {
        UserIds = userIds;
    }
}
