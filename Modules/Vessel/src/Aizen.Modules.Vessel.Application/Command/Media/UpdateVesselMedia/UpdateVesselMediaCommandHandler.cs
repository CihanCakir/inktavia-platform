using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Update Vessel Media Command Handler", "Loads media, validates file if changed, applies metadata update, invalidates media cache and publishes VesselMediaUpdatedMessage.")]
public sealed class UpdateVesselMediaCommandHandler : AizenCommandHandler<UpdateVesselMediaCommand, UpdateVesselMediaResponse>
{
    private static readonly IReadOnlyList<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<UpdateVesselMediaCommandHandler> _logger;

    public UpdateVesselMediaCommandHandler(
        IVesselMediaRepository mediaRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IVesselFileStorageService fileStorage,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info,
        ILogger<UpdateVesselMediaCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _fileStorage = fileStorage;
        _publisher = publisher;
        _info = info;
        _logger = logger;
    }

    public override async Task<UpdateVesselMediaResponse?> Handle(UpdateVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles.Contains("Admin");
        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        var media = await _mediaRepository.GetByIdWithVesselAsync(request.MediaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(media.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        string? originalFileNameSnapshot = media.OriginalFileNameSnapshot;
        string? contentTypeSnapshot = media.ContentTypeSnapshot;
        long? sizeInBytesSnapshot = media.SizeInBytesSnapshot;
        bool fileChanged = r.FileId.HasValue && r.FileId != media.FileId;

        if (fileChanged)
        {
            await _fileStorage.EnsureFileCanBeAttachedToVesselAsync(r.FileId!.Value, AllowedContentTypes, accessToken, cancellationToken);
            var meta = await _fileStorage.GetRequiredFileMetadataAsync(r.FileId.Value, accessToken, cancellationToken);
            originalFileNameSnapshot = meta.OriginalFileName;
            contentTypeSnapshot = meta.ContentType;
            sizeInBytesSnapshot = meta.SizeInBytes;
        }

        bool coverChanged = r.IsCover && !media.IsCover;
        if (coverChanged)
            await _mediaRepository.UnsetAllCoversAsync(media.VesselId, cancellationToken);

        media.Update(
            r.MediaType,
            r.FileId,
            originalFileNameSnapshot,
            contentTypeSnapshot,
            sizeInBytesSnapshot,
            r.SortOrder,
            r.IsCover);
        _mediaRepository.Update(media);

        if (fileChanged)
        {
            try
            {
                await _fileStorage.LinkFileToVesselOwnerAsync(r.FileId!.Value, media.Id, "VesselMedia", accessToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to link file {FileId} to media {MediaId}. Continuing.", r.FileId, media.Id);
            }
        }

        await _invalidation.InvalidateMediaAsync(media.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselMediaUpdatedMessage
        {
            VesselId = media.VesselId,
            MediaId = media.Id,
            ActorUserId = currentUserId
        }, cancellationToken);

        if (coverChanged)
        {
            await _publisher.PublishAsync(new VesselCoverMediaChangedMessage
            {
                VesselId = media.VesselId,
                CoverMediaId = media.Id,
                ActorUserId = currentUserId
            }, cancellationToken);
        }

        return new UpdateVesselMediaResponse(media.ToDto());
    }
}
