using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Request.Location;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Records the auto-detected CURRENT position. Resolve (sets the identity holder so the module write asserts as
/// this owner) → ownership gate against the caller's default owned page → append a new location snapshot via the
/// module (Source="device"). Returns the stored snapshot as the mobile current-location shape.
/// </summary>
public sealed class UpdateMobileVesselCurrentLocationCommandHandler
    : AizenCommandHandler<UpdateMobileVesselCurrentLocationCommand, MobileVesselLocationDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;
    private const string DeviceSource = "device";

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public UpdateMobileVesselCurrentLocationCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselLocationDto?> Handle(
        UpdateMobileVesselCurrentLocationCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Request ?? throw new AizenBusinessException("Location payload is required.");
        if (payload.Lat is null || payload.Lng is null)
            throw new AizenBusinessException("Both 'lat' and 'lng' are required.");
        if (payload.Lat is < -90 or > 90)
            throw new AizenBusinessException("'lat' must be between -90 and 90.");
        if (payload.Lng is < -180 or > 180)
            throw new AizenBusinessException("'lng' must be between -180 and 180.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var resp = await _vessel.UpdateVesselLocation(request.VesselId, new UpdateVesselLocationSnapshotRequest
        {
            Latitude = (decimal)payload.Lat.Value,
            Longitude = (decimal)payload.Lng.Value,
            Source = DeviceSource,
        });

        var snap = resp?.Body?.Snapshot;
        if (snap is null)
            throw new AizenBusinessException("Location could not be recorded.");

        return new MobileVesselLocationDto
        {
            MarinaName = snap.MarinaName,
            Latitude = (double?)snap.Latitude,
            Longitude = (double?)snap.Longitude,
            CapturedAt = snap.CapturedAt,
        };
    }
}
