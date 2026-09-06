using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Request.Location;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Sets the owner's EXPLICIT location choice. Resolve → ownership gate → (when a MarinaId is given) resolve the
/// marina's name + coordinates from ReferenceData and denormalize them into the Vessel-module write, so the Vessel
/// module never has to call ReferenceData. An all-null body clears the selection. Returns the stored selection.
/// </summary>
public sealed class SetMobileVesselSelectedLocationCommandHandler
    : AizenCommandHandler<SetMobileVesselSelectedLocationCommand, MobileSelectedLocationDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IReferenceDataRemoteCall _referenceData;

    public SetMobileVesselSelectedLocationCommandHandler(
        IParticipantProfileResolver resolver, IVesselRemoteCall vessel, IReferenceDataRemoteCall referenceData)
    {
        _resolver = resolver;
        _vessel = vessel;
        _referenceData = referenceData;
    }

    public override async Task<MobileSelectedLocationDto?> Handle(
        SetMobileVesselSelectedLocationCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Request ?? throw new AizenBusinessException("Selected-location payload is required.");
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

        var moduleRequest = new SetVesselSelectedLocationRequest
        {
            CustomLabel = payload.CustomLabel,
            Latitude = (decimal?)payload.Lat,
            Longitude = (decimal?)payload.Lng,
        };

        // Marina pick → resolve name + coords once, here, and denormalize into the write. Coordinates the client
        // supplied win; otherwise fall back to the marina's own position.
        if (payload.MarinaId is > 0)
        {
            var marina = (await _referenceData.GetMarinaById(payload.MarinaId.Value))?.Body
                ?? throw new AizenBusinessException("Marina not found.");

            moduleRequest.MarinaId = marina.Id;
            moduleRequest.MarinaName = marina.Name;
            moduleRequest.Latitude ??= marina.Latitude;
            moduleRequest.Longitude ??= marina.Longitude;
        }

        var resp = await _vessel.SetVesselSelectedLocation(request.VesselId, moduleRequest);
        var sel = resp?.Body?.SelectedLocation;
        if (sel is null)
            return null; // cleared

        return new MobileSelectedLocationDto
        {
            MarinaId = sel.MarinaId,
            MarinaName = sel.MarinaName,
            CustomLabel = sel.CustomLabel,
            Latitude = (double?)sel.Latitude,
            Longitude = (double?)sel.Longitude,
            SetAt = sel.SetAt,
        };
    }
}
