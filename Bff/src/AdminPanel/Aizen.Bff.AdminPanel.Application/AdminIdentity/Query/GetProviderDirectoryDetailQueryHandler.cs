using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

/// <summary>
/// Phase 27 — Returns organizer/provider detail with linked user data.
/// Calls the admin long-ID remote call (Identity module uses long PK, not Guid).
/// </summary>
[DocumentationInfo(
    "GetProviderDirectoryDetail query handler",
    "Returns OrganizerProfileWithUserDetailDto for the provider directory detail page. " +
    "Uses GetAdminOrganizerProfileWithUser(long) which maps to Identity module's {profileId:long} route.")]
public sealed class GetProviderDirectoryDetailQueryHandler
    : AizenQueryHandler<GetProviderDirectoryDetailQuery, OrganizerProfileWithUserDetailDto>
{
    private readonly IIdentityRemoteCall _identity;

    public GetProviderDirectoryDetailQueryHandler(IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<OrganizerProfileWithUserDetailDto?> Handle(
        GetProviderDirectoryDetailQuery request, CancellationToken ct)
    {
        var r = await _identity.GetAdminOrganizerProfileWithUser(request.ProfileId);
        return r.Body;
    }
}
