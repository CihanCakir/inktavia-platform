using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetAdminProfileDetailQuery : AizenQuery<ProfileDetailResult>
{
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public string RoleContext { get; }
    public GetAdminProfileDetailQuery(Guid profileId, string authorization, string roleContext)
    {
        ProfileId = profileId;
        Authorization = authorization;
        RoleContext = roleContext;
    }
}

[DocumentationInfo("Get admin profile detail query handler", "Retrieves profile detail for a given user. Delegates to the general profile endpoint; role-context is preserved for caller usage.")]
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
        var result = await _identity.GetProfileById(request.ProfileId, request.Authorization);
        return result.Body;
    }
}
