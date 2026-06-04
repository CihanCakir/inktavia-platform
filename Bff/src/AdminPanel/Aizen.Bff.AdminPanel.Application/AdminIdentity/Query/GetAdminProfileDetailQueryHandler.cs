using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("Get admin profile detail query handler", "Retrieves profile detail for a given user via the Identity module.")]
public sealed class GetAdminProfileDetailQueryHandler
    : AizenQueryHandler<GetAdminProfileDetailQuery, ProfileDetailResult>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public GetAdminProfileDetailQueryHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ProfileDetailResult?> Handle(
        GetAdminProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var result = await _identity.GetProfileById(request.ProfileId, request.Authorization, request.UserToken);
        return result.Body;
    }
}
