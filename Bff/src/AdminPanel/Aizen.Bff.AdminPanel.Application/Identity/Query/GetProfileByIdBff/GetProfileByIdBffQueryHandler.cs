using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetProfileById query handler", "Returns a user profile by its ID from the Identity module.")]
public sealed class GetProfileByIdBffQueryHandler : AizenQueryHandler<GetProfileByIdBffQuery, ProfileDetailResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetProfileByIdBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ProfileDetailResult?> Handle(GetProfileByIdBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetProfileById(request.ProfileId);
        return r.Body;
    }
}
