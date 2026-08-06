using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

[DocumentationInfo("GetParticipantProfileById query handler", "Returns a single participant profile by ID from the Identity module.")]
public sealed class GetParticipantProfileByIdQueryHandler : AizenQueryHandler<GetParticipantProfileByIdQuery, ParticipantProfileResult>
{
    private readonly IIdentityRemoteCall _identity;
    public GetParticipantProfileByIdQueryHandler(
        IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<ParticipantProfileResult?> Handle(GetParticipantProfileByIdQuery request, CancellationToken ct)
    {

        var r = await _identity.GetParticipantProfileById(request.ProfileId);
        return r.Body;
    }
}
