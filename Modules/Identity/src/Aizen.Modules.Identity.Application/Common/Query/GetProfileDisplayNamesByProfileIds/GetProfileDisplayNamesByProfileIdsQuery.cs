using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

/// <summary>
/// BE-MO2c — batch resolve profile ids → display names (CompanyName-first). Minimal projection; safe to serve at
/// IdentityRead. One query, no N+1. The reusable primitive for BFF-side provider-name enrichment.
/// </summary>
public sealed class GetProfileDisplayNamesByProfileIdsQuery : AizenListedQuery<ProfileDisplayNameDto>
{
    public long[] ProfileIds { get; }

    public GetProfileDisplayNamesByProfileIdsQuery(long[] profileIds)
    {
        ProfileIds = profileIds ?? System.Array.Empty<long>();
    }
}
