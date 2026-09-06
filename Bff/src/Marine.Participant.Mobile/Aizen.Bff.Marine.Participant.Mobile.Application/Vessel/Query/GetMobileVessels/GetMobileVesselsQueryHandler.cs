using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

public sealed class GetMobileVesselsQueryHandler
    : AizenQueryHandler<GetMobileVesselsQuery, List<MobileVesselListItemDto>>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetMobileVesselsQueryHandler> _logger;

    public GetMobileVesselsQueryHandler(
        IParticipantProfileResolver resolver, IVesselRemoteCall vessel, ILogger<GetMobileVesselsQueryHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<List<MobileVesselListItemDto>?> Handle(
        GetMobileVesselsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve → sets the identity holder so `current-user` scopes to this participant's UserId.
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            if (resolution.ProfileId is not > 0)
                return new List<MobileVesselListItemDto>();

            // Read the module's DEFAULT page (0,20) — the exact key InvalidateUserVesselListAsync evicts on a
            // write — so a just-created/updated vessel appears immediately (the read is cached per page tuple and
            // the invalidator can only evict the default page). A participant owns a handful of vessels, so the
            // default page covers the whole set; broader ownership would need real pagination + wider invalidation.
            var resp = await _vessel.GetUserVessels(0, 20);
            var items = resp?.Body?.Vessels?.Items ?? new List<VesselListItemDto>();
            // Active list only (M4d): archived vessels stay in the module read model (the query filters only by
            // ownership) but must drop out of the mobile list / Home / picker. Archive + restore both invalidate
            // this default page, so the transition shows immediately.
            return items.Where(v => !v.IsArchived).Select(MapListItem).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Participant vessels list failed.");
            return new List<MobileVesselListItemDto>();
        }
    }

    internal static MobileVesselListItemDto MapListItem(VesselListItemDto v)
    {
        // A selection is "set" only when it carries a value (the module projects the object unconditionally).
        var sel = v.SelectedLocation;
        var hasSelection = sel is not null && (sel.MarinaId.HasValue
            || !string.IsNullOrWhiteSpace(sel.MarinaName)
            || !string.IsNullOrWhiteSpace(sel.CustomLabel)
            || sel.Latitude.HasValue
            || sel.Longitude.HasValue);

        var selected = hasSelection ? new MobileSelectedLocationDto
        {
            MarinaId = sel!.MarinaId,
            MarinaName = sel.MarinaName,
            CustomLabel = sel.CustomLabel,
            Latitude = (double?)sel.Latitude,
            Longitude = (double?)sel.Longitude,
            SetAt = sel.SetAt,
        } : null;

        var displayName = selected?.MarinaName ?? selected?.CustomLabel ?? v.LastLocationMarinaName;

        return new MobileVesselListItemDto
        {
            Id = v.Id,
            Name = v.Name,
            TypeCode = v.VesselTypeCode,
            Flag = v.FlagCountryCode,
            Status = v.Status.ToString(),
            CoverMediaUrl = v.CoverMediaUrl,
            LengthMeters = v.LengthMeters,
            GrossTonnage = v.GrossTonnage,
            MarinaName = v.LastLocationMarinaName,
            SelectedLocation = selected,
            DisplayLocationName = displayName,
        };
    }
}
