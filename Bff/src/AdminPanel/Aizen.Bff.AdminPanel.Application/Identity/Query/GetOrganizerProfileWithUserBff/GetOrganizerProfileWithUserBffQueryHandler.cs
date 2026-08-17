using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetOrganizerProfileWithUser query handler", "Returns organizer profile with linked user details from the Identity module.")]
public sealed class GetOrganizerProfileWithUserBffQueryHandler : AizenQueryHandler<GetOrganizerProfileWithUserBffQuery, OrganizerProfileWithUserResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetOrganizerProfileWithUserBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<OrganizerProfileWithUserResult?> Handle(GetOrganizerProfileWithUserBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetOrganizerProfileWithUser(request.ProfileId);
        return r.Body;
    }
}
