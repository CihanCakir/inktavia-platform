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

            var resp = await _vessel.GetUserVessels(0, 100);
            var items = resp?.Body?.Vessels?.Items ?? new List<VesselListItemDto>();
            return items.Select(MapListItem).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Participant vessels list failed.");
            return new List<MobileVesselListItemDto>();
        }
    }

    internal static MobileVesselListItemDto MapListItem(VesselListItemDto v) => new()
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
    };
}
