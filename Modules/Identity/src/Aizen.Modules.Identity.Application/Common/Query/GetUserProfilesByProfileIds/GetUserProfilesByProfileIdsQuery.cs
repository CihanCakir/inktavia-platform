using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

/// <summary>Admin bulk lookup of user profiles by a list of profile IDs. Used as fallback by BFF enrichment when userId lookup returns no results.</summary>
public sealed class GetUserProfilesByProfileIdsQuery : AizenListedQuery<UserProfileListItemDto>
{
    public long[] ProfileIds { get; }

    public GetUserProfilesByProfileIdsQuery(long[] profileIds)
    {
        ProfileIds = profileIds;
    }
}
