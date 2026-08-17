using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

[DocumentationInfo("GetParticipantProfileById query handler", "Returns a single participant profile by ID from the Identity module.")]
public sealed class GetParticipantProfileByIdBffQueryHandler : AizenQueryHandler<GetParticipantProfileByIdBffQuery, ParticipantProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetParticipantProfileByIdBffQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ParticipantProfileResult?> Handle(GetParticipantProfileByIdBffQuery request, CancellationToken ct)
    {

        var r = await _identity.GetParticipantProfileById(request.ProfileId);
        return r.Body;
    }
}
