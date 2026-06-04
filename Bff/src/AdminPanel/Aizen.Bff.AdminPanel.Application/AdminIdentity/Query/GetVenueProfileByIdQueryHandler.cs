using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetVenueProfileById query handler", "Returns a single venue profile by ID from the Identity module.")]
public sealed class GetVenueProfileByIdQueryHandler : AizenQueryHandler<GetVenueProfileByIdQuery, VenueProfileResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    public GetVenueProfileByIdQueryHandler(IIdentityAdminBffRemoteCall identity) { _identity = identity; }

    public override async Task<VenueProfileResult?> Handle(GetVenueProfileByIdQuery request, CancellationToken ct)
    {
        var r = await _identity.GetVenueProfileById(request.ProfileId, request.Authorization, request.UserToken);
        return r.Body;
    }
}
