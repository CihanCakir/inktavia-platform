using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetVenueProfileById query handler", "Returns a single venue profile by ID from the Identity module.")]
public sealed class GetVenueProfileByIdBffQueryHandler : AizenQueryHandler<GetVenueProfileByIdBffQuery, VenueProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetVenueProfileByIdBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<VenueProfileResult?> Handle(GetVenueProfileByIdBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetVenueProfileById(request.ProfileId);
        return r.Body;
    }
}
