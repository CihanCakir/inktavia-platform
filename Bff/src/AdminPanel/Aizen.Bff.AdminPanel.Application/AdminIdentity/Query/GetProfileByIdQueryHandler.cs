using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetProfileById query handler", "Returns a user profile by its ID from the Identity module.")]
public sealed class GetProfileByIdQueryHandler : AizenQueryHandler<GetProfileByIdQuery, ProfileDetailResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetProfileByIdQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ProfileDetailResult?> Handle(GetProfileByIdQuery request, CancellationToken ct)
    {

        var r = await _identity.GetProfileById(request.ProfileId);
        return r.Body;
    }
}
