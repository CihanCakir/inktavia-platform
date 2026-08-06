using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Delete a vessel photo (M4f). Owner-gated + media-membership pre-checked (the module returns a raw 500 for a
/// missing media id), so a foreign/unknown id yields a clean not-found. The module deactivates + invalidates the
/// media/list/detail caches, so the gallery drops it immediately.
/// </summary>
public sealed class DeleteMobileVesselMediaCommandHandler
    : AizenCommandHandler<DeleteMobileVesselMediaCommand, MobileVesselMediaDeletedDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public DeleteMobileVesselMediaCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselMediaDeletedDto?> Handle(
        DeleteMobileVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var mediaResp = await _vessel.GetVesselMedia(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        var exists = mediaResp?.Body?.Media?.Items?.Any(m => m.Id == request.MediaId && m.IsActive) ?? false;
        if (!exists)
            throw new AizenBusinessException("Photo not found.");

        var resp = await _vessel.RemoveVesselMedia(request.VesselId, request.MediaId);
        if (resp?.Body is null)
            throw new AizenBusinessException("Photo not found.");

        return new MobileVesselMediaDeletedDto { VesselId = resp.Body.VesselId, MediaId = resp.Body.MediaId };
    }
}
