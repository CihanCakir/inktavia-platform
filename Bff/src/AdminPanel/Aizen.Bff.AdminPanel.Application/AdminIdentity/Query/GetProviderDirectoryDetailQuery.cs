using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

/// <summary>
/// Phase 27 — Provider Directory Detail query.
/// Uses long profileId (matching Identity module's numeric PK) instead of Guid,
/// because the Identity module exposes {profileId:long} routes.
/// </summary>
public sealed class GetProviderDirectoryDetailQuery : AizenQuery<OrganizerProfileWithUserDetailDto>
{
    public long ProfileId { get; }

    public GetProviderDirectoryDetailQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
