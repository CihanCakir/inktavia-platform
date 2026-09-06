using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Create orchestration (BE_MO1). The module has no cross-call transaction, so this composes:
///   1) POST /service-requests — REQUIRED. Creates the Draft with the asserted caller as OwnerUserId.
///   2) attach any already-uploaded files (best-effort).
///   3) PATCH /publish (best-effort) when the wizard submitted (Draft → Open).
/// Step 1 is authoritative. The module builds its create response from the entity BEFORE the unit-of-work commits,
/// so the returned Id is 0 — the row IS committed by the time this returns, so the real Id is recovered
/// deterministically by the just-created request's unique RequestCode from the owner's list (an owner has a small
/// set, and the list is uncached, so the fresh row is present). Steps 2–3 are best-effort — a failure is logged,
/// and the returned detail reflects exactly what persisted, so the client never sees a success that hides a
/// dropped attachment or an unpublished request.
/// </summary>
public sealed class CreateMobileServiceRequestCommandHandler
    : AizenCommandHandler<CreateMobileServiceRequestCommand, MobileServiceRequestDetailDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 50;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IVesselRemoteCall _vessel;
    private readonly IReferenceDataRemoteCall _referenceData;
    private readonly ILogger<CreateMobileServiceRequestCommandHandler> _logger;

    public CreateMobileServiceRequestCommandHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        IVesselRemoteCall vessel,
        IReferenceDataRemoteCall referenceData,
        ILogger<CreateMobileServiceRequestCommandHandler> logger)
    {
        _resolver = resolver;
        _sr = sr;
        _fileStorage = fileStorage;
        _vessel = vessel;
        _referenceData = referenceData;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestDetailDto?> Handle(
        CreateMobileServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request ?? throw new AizenBusinessException("Service request payload is required.");

        if (r.VesselId <= 0)
            throw new AizenBusinessException("A vessel is required.");
        if (string.IsNullOrWhiteSpace(r.ServiceCategoryCode))
            throw new AizenBusinessException("A service category is required.");
        if (string.IsNullOrWhiteSpace(r.Title))
            throw new AizenBusinessException("A title is required.");

        // Resolve → sets the identity holder so the create asserts as this participant (OwnerUserId = caller).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Job location = the vessel's location. When the client omits it, default from the vessel's selectedLocation
        // (owner's explicit choice), falling back to currentLocation (auto-detected) — realizing the Phase-1
        // "selectedLocation preferred, fallback currentLocation" source. When the client omits the city AND the
        // vessel's selected location references a marina, default LocationCityCode from that marina's cityCode
        // (Task 7) so the city:{code} SignalR fan-out + provider city matching work from vessel location with zero
        // extra input. Owner's own vessel, so no leak. Best-effort: any failure leaves fields null (null-safe).
        var (locLat, locLng, locCity) = await ResolveJobLocationAsync(
            r.VesselId, r.LocationLatitude, r.LocationLongitude, r.LocationCityCode, cancellationToken);

        // 1) Create the Draft (required).
        var createResp = await _sr.Create(new CreateServiceRequestRequest
        {
            VesselId = r.VesselId,
            ServiceCategoryCode = r.ServiceCategoryCode.Trim(),
            ServiceTypeCode = NullIfBlank(r.ServiceTypeCode),
            Title = r.Title.Trim(),
            Description = NullIfBlank(r.Description),
            Priority = MobileServiceRequestMapper.ParsePriority(r.Priority),
            RequestedStartDate = r.RequestedStartDate,
            RequestedEndDate = r.RequestedEndDate,
            LocationCountryCode = NullIfBlank(r.LocationCountryCode),
            LocationCityCode = NullIfBlank(locCity),
            LocationMarinaName = NullIfBlank(r.LocationMarinaName),
            LocationLatitude = locLat,
            LocationLongitude = locLng,
            OwnerNotes = NullIfBlank(r.OwnerNotes),
        });

        var created = createResp?.Body?.ServiceRequest;
        if (created is null || string.IsNullOrWhiteSpace(created.RequestCode))
            throw new AizenBusinessException("Service request could not be created.");

        // Recover the committed id by the just-created request's unique RequestCode (create response Id is 0 pre-commit).
        var serviceRequestId = created.Id;
        if (serviceRequestId <= 0)
        {
            var mine = await _sr.GetMy(new Aizen.Modules.ServiceRequest.Abstraction.Request.Filter.ServiceRequestListFilterRequest
            {
                PageIndex = OwnedPageIndex,
                PageSize = OwnedPageSize,
            });
            serviceRequestId = mine?.Body?.Items?
                .FirstOrDefault(x => x.RequestCode == created.RequestCode)?.Id ?? 0;
        }
        if (serviceRequestId <= 0)
            throw new AizenBusinessException("Service request could not be created.");

        // 2) Attachments (optional, best-effort) — files already uploaded client-side via /mobile/uploads.
        foreach (var att in r.Attachments ?? Enumerable.Empty<MobileServiceRequestAttachmentInput>())
        {
            if (att.FileId == Guid.Empty) continue;
            try
            {
                await _sr.AddAttachment(serviceRequestId, new AddServiceRequestAttachmentRequest
                {
                    FileId = att.FileId,
                    AttachmentType = MobileServiceRequestMapper.ParseAttachmentType(att.AttachmentType),
                    Title = NullIfBlank(att.Title),
                    Description = NullIfBlank(att.Description),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service request {ServiceRequestId} created but attachment {FileId} failed.", serviceRequestId, att.FileId);
            }
        }

        // 3) Publish (optional, best-effort) — submit the Draft to providers (Draft → Open).
        if (r.Publish)
        {
            try
            {
                await _sr.Publish(serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service request {ServiceRequestId} created but publish failed (stays Draft).", serviceRequestId);
            }
        }

        // Return the assembled detail so the client sees exactly what persisted.
        var detailResp = await _sr.GetDetail(serviceRequestId);
        var detail = detailResp?.Body?.Detail;
        if (detail is not null)
            return await MobileServiceRequestMapper.MapDetailWithAttachmentUrlsAsync(detail, _fileStorage, _logger, cancellationToken);

        // Detail read failed post-create — echo a minimal projection rather than 500.
        return new MobileServiceRequestDetailDto
        {
            Id = serviceRequestId,
            RequestCode = created.RequestCode,
            VesselId = created.VesselId,
            ServiceCategoryCode = created.ServiceCategoryCode,
            ServiceTypeCode = created.ServiceTypeCode,
            Title = created.Title,
            Description = created.Description,
            Status = created.Status.ToString(),
            Priority = created.Priority.ToString(),
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CanEdit = true,
            CanCancel = true,
        };
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Job location + city from the vessel. Coords: client-supplied when present, else the vessel's
    /// selectedLocation (preferred) → currentLocation (whole-source preference, never mixing). City: client-supplied
    /// when present, else the cityCode of the marina referenced by the vessel's selectedLocation (Task 7). One vessel
    /// fetch; the marina is fetched only when the city is needed and a marina is referenced. Best-effort — returns
    /// whatever was supplied on any failure so create still succeeds.</summary>
    private async Task<(decimal? Lat, decimal? Lng, string? CityCode)> ResolveJobLocationAsync(
        long vesselId, decimal? suppliedLat, decimal? suppliedLng, string? suppliedCityCode, CancellationToken ct)
    {
        var lat = suppliedLat;
        var lng = suppliedLng;
        var cityCode = NullIfBlank(suppliedCityCode);

        var needCoords = !(lat.HasValue && lng.HasValue);
        var needCity = string.IsNullOrWhiteSpace(cityCode);
        if (!needCoords && !needCity)
            return (lat, lng, cityCode);

        try
        {
            var detail = (await _vessel.GetVesselDetail(vesselId))?.Body?.Vessel;
            var sel = detail?.Vessel?.SelectedLocation;

            if (needCoords)
            {
                if (sel?.Latitude is not null && sel.Longitude is not null)
                {
                    lat = sel.Latitude;
                    lng = sel.Longitude;
                }
                else
                {
                    var cur = detail?.CurrentLocation;
                    if (cur?.Latitude is not null && cur.Longitude is not null)
                    {
                        lat = cur.Latitude;
                        lng = cur.Longitude;
                    }
                }
            }

            // Default the city from the selected marina's cityCode (owner can still override by supplying one).
            if (needCity && sel?.MarinaId is > 0)
            {
                var marina = (await _referenceData.GetMarinaById(sel.MarinaId.Value))?.Body;
                if (!string.IsNullOrWhiteSpace(marina?.CityCode))
                    cityCode = marina!.CityCode;
            }

            // STILL no city but we have coordinates (custom map pin / bare coords) → derive from the NEAREST marina's
            // cityCode within a sanity cap. Closes the fan-out gap (city:{code} SignalR + provider city web-push).
            // Never overrides — only fills when cityCode is null (guarded by SrCityDerivation).
            if (string.IsNullOrWhiteSpace(cityCode) && lat.HasValue && lng.HasValue)
            {
                var nearby = (await _referenceData.GetNearbyMarinas((double)lat.Value, (double)lng.Value, 1))?.Body;
                var nearest = nearby is { Count: > 0 } ? nearby[0] : null;
                var derived = SrCityDerivation.ResolveCityCode(cityCode, nearest?.CityCode, nearest?.DistanceMeters);
                if (!string.IsNullOrWhiteSpace(derived))
                    cityCode = derived;
                else if (nearest is not null)
                    _logger.LogDebug(
                        "SR create: nearest marina '{Name}' is {Dist:0}m from the pin (> {Cap:0}m cap) — city not derived.",
                        nearest.Name, nearest.DistanceMeters, SrCityDerivation.MaxDeriveDistanceMeters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vessel {VesselId} location/city lookup failed for SR create; proceeding with supplied values.", vesselId);
        }

        return (lat, lng, cityCode);
    }
}
