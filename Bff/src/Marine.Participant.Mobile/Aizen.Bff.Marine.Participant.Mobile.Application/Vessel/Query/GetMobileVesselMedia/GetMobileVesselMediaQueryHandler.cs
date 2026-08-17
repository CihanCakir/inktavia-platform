using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// List a vessel's photos (M4f). Owner-gated (clean not-found for a foreign/unknown id). Reads the module's media
/// DEFAULT page with includeAccessUrls=false — the exact key InvalidateMediaAsync evicts on add/remove/set-cover,
/// so changes show immediately — then resolves each item's presigned URL per-item. Active only + cover first.
/// </summary>
public sealed class GetMobileVesselMediaQueryHandler
    : AizenQueryHandler<GetMobileVesselMediaQuery, List<MobileVesselMediaDto>>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileVesselMediaQueryHandler> _logger;

    public GetMobileVesselMediaQueryHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileVesselMediaQueryHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<List<MobileVesselMediaDto>?> Handle(
        GetMobileVesselMediaQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var resp = await _vessel.GetVesselMedia(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        var items = (resp?.Body?.Media?.Items
            ?? Enumerable.Empty<Aizen.Modules.Vessel.Abstraction.Dto.Media.VesselMediaDto>())
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.IsCover).ThenBy(m => m.SortOrder)
            .ToList();

        var result = new List<MobileVesselMediaDto>(items.Count);
        foreach (var m in items)
            result.Add(await MobileVesselMediaMapper.MapWithUrlAsync(m, _fileStorage, _logger, cancellationToken));
        return result;
    }
}
