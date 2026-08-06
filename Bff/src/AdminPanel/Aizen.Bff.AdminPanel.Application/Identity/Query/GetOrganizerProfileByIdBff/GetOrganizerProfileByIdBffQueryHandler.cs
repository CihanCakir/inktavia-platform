using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetOrganizerProfileById query handler", "Returns a single organizer profile by ID from the Identity module.")]
public sealed class GetOrganizerProfileByIdBffQueryHandler : AizenQueryHandler<GetOrganizerProfileByIdBffQuery, OrganizerProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetOrganizerProfileByIdBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<OrganizerProfileResult?> Handle(GetOrganizerProfileByIdBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetOrganizerProfileById(request.ProfileId);
        return r.Body;
    }
}
