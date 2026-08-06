using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Attach a completed upload as a vessel photo (M4f). Owner-gated (clean not-found). The module validates the file
/// via FileStorage (the S2S path fixed in M4e), links it, and invalidates the media + detail + user-list caches.
/// The first photo is auto-covered so the list/Home card immediately has a cover. Returns the created media.
/// </summary>
public sealed class AttachMobileVesselMediaCommandHandler
    : AizenCommandHandler<AttachMobileVesselMediaCommand, MobileVesselMediaDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<AttachMobileVesselMediaCommandHandler> _logger;

    public AttachMobileVesselMediaCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        ILogger<AttachMobileVesselMediaCommandHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileVesselMediaDto?> Handle(
        AttachMobileVesselMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Request is null || request.Request.FileId == Guid.Empty)
            throw new AizenBusinessException("A fileId is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        // First active photo → auto-cover (so the list/Home card has a cover from the very first upload).
        var existing = await _vessel.GetVesselMedia(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        var hasActive = existing?.Body?.Media?.Items?.Any(m => m.IsActive) ?? false;
        var isCover = request.Request.IsCover ?? !hasActive;

        var addResp = await _vessel.AddVesselMedia(request.VesselId, new AddVesselMediaRequest
        {
            MediaType = VesselMediaType.Photo,
            FileId = request.Request.FileId,
            SortOrder = 0,
            IsCover = isCover,
        });

        var created = addResp?.Body?.Media
            ?? throw new AizenBusinessException("Photo uploaded but could not be attached to the vessel.");

        return await MobileVesselMediaMapper.MapWithUrlAsync(created, _fileStorage, _logger, cancellationToken);
    }
}
