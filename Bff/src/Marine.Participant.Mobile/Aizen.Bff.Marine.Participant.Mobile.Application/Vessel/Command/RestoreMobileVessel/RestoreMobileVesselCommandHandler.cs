using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Restore orchestration (M4d). Owner-gated pass-through to the module's <c>PATCH /vessels/{id}/restore</c>.
/// An archived vessel is still present in the module's (ownership-only) read model, so the up-front gate against
/// the caller's default-page owned set resolves it; a foreign/unknown id returns a clean not-found. The module
/// clears IsArchived + re-invalidates the default user list page, so the vessel reappears in the active list
/// immediately; this handler returns the re-read detail projection (IsArchived = false).
/// </summary>
public sealed class RestoreMobileVesselCommandHandler
    : AizenCommandHandler<RestoreMobileVesselCommand, MobileVesselDetailDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public RestoreMobileVesselCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        RestoreMobileVesselCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var restoreResp = await _vessel.RestoreVessel(request.VesselId);
        if (restoreResp?.Body is null)
            throw new AizenBusinessException("Vessel could not be restored.");

        var detail = (await _vessel.GetVesselDetail(request.VesselId))?.Body?.Vessel;
        if (detail is not null)
            return MobileVesselMapper.MapDetail(detail);

        throw new AizenBusinessException("Vessel restored but detail could not be read.");
    }
}
