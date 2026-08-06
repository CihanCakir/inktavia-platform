using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("Get admin profile detail query handler", "Retrieves profile detail for a given user via the Identity module.")]
public sealed class GetProfileDetailBffQueryHandler
    : AizenQueryHandler<GetProfileDetailBffQuery, ProfileDetailResult>
{
    private readonly IIdentityRemoteCall _identity;

    public GetProfileDetailBffQueryHandler(IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ProfileDetailResult?> Handle(
        GetProfileDetailBffQuery request, CancellationToken cancellationToken)
    {

        var result = await _identity.GetProfileById(request.ProfileId);
        return result.Body;
    }
}
