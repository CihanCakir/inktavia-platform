using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetProfileWithRoles query handler", "Returns a user profile with its assigned roles from the Identity module.")]
public sealed class GetProfileWithRolesBffQueryHandler : AizenQueryHandler<GetProfileWithRolesBffQuery, ProfileWithRolesResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetProfileWithRolesBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ProfileWithRolesResult?> Handle(GetProfileWithRolesBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetProfileWithRoles(request.ProfileId);
        return r.Body;
    }
}
