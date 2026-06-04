using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetOrganizerProfileWithUser query handler", "Returns organizer profile with linked user details from the Identity module.")]
public sealed class GetOrganizerProfileWithUserQueryHandler : AizenQueryHandler<GetOrganizerProfileWithUserQuery, OrganizerProfileWithUserResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public GetOrganizerProfileWithUserQueryHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<OrganizerProfileWithUserResult?> Handle(GetOrganizerProfileWithUserQuery request, CancellationToken ct)
    {
        var r = await _identity.GetOrganizerProfileWithUser(request.ProfileId, request.Authorization, request.UserToken);
        return r.Body;
    }
}
