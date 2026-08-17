using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Set a photo as the vessel cover (M4f). Owner-gated + media-membership pre-checked (clean not-found). The module
/// clears other covers and invalidates the media + detail + user-list caches, so the list/Home cover updates
/// immediately. Returns the re-read gallery reflecting the new cover.
/// </summary>
public sealed class SetCoverMobileVesselMediaCommandHandler
    : AizenCommandHandler<SetCoverMobileVesselMediaCommand, List<MobileVesselMediaDto>>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<SetCoverMobileVesselMediaCommandHandler> _logger;

    public SetCoverMobileVesselMediaCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        ILogger<SetCoverMobileVesselMediaCommandHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<List<MobileVesselMediaDto>?> Handle(
        SetCoverMobileVesselMediaCommand request, CancellationToken cancellationToken)
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

        var resp = await _vessel.SetCoverVesselMedia(request.VesselId, request.MediaId);
        if (resp?.Body is null)
            throw new AizenBusinessException("Cover could not be set.");

        // Re-read the (now-invalidated) media page and project with fresh URLs + cover flags.
        var refreshed = await _vessel.GetVesselMedia(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        var items = (refreshed?.Body?.Media?.Items
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
