using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetOrganizerProfileById query handler", "Returns a single organizer profile by ID from the Identity module.")]
public sealed class GetOrganizerProfileByIdQueryHandler : AizenQueryHandler<GetOrganizerProfileByIdQuery, OrganizerProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public GetOrganizerProfileByIdQueryHandler(
        IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<OrganizerProfileResult?> Handle(GetOrganizerProfileByIdQuery request, CancellationToken ct)
    {

        var r = await _identity.GetOrganizerProfileById(request.ProfileId);
        return r.Body;
    }
}
