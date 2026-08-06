using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Detail with an OWNERSHIP GATE: the module detail endpoint has no owner check, so the BFF first resolves the
/// caller's owned-vessel set (asserted `current-user`) and only returns detail for an id in that set — otherwise
/// a clean "not found" business error (never another participant's vessel, never a 500).
/// </summary>
public sealed class GetMobileVesselDetailQueryHandler
    : AizenQueryHandler<GetMobileVesselDetailQuery, MobileVesselDetailDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetMobileVesselDetailQueryHandler> _logger;

    public GetMobileVesselDetailQueryHandler(
        IParticipantProfileResolver resolver, IVesselRemoteCall vessel, ILogger<GetMobileVesselDetailQueryHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        GetMobileVesselDetailQuery request, CancellationToken cancellationToken)
    {
        // Resolve → sets the identity holder (scopes the ownership lookup); no profile ⇒ owns nothing.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("Vessel not found.");

        var listResp = await _vessel.GetUserVessels(0, 200);
        var owned = listResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owned)
            throw new AizenBusinessException("Vessel not found.");

        var detailResp = await _vessel.GetVesselDetail(request.VesselId);
        var d = detailResp?.Body?.Vessel;
        if (d is null)
            throw new AizenBusinessException("Vessel not found.");

        return MobileVesselMapper.MapDetail(d);
    }
}
