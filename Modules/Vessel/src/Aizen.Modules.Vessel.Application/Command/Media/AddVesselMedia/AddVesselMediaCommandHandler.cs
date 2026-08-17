using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Add Vessel Media Command Handler", "Validates the file via FileStorage, creates a vessel media entity, links the file, invalidates media cache and publishes VesselMediaAddedMessage.")]
public sealed class AddVesselMediaCommandHandler : AizenCommandHandler<AddVesselMediaCommand, AddVesselMediaResponse>
{
    private static readonly IReadOnlyList<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AddVesselMediaCommandHandler> _logger;

    public AddVesselMediaCommandHandler(
        IVesselMediaRepository mediaRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IVesselFileStorageService fileStorage,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AddVesselMediaCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _fileStorage = fileStorage;
        _publisher = publisher;
        _info = info;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task<AddVesselMediaResponse?> Handle(AddVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        // Roles is empty on a BFF assertion S2S call; fall back to the framework principal (service token carries Admin).
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles?.Contains("Admin") == true
            || _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        string? originalFileNameSnapshot = null;
        string? contentTypeSnapshot = null;
        long? sizeInBytesSnapshot = null;

        if (r.FileId.HasValue)
        {
            await _fileStorage.EnsureFileCanBeAttachedToVesselAsync(r.FileId.Value, AllowedContentTypes, accessToken, cancellationToken);
            var meta = await _fileStorage.GetRequiredFileMetadataAsync(r.FileId.Value, accessToken, cancellationToken);
            originalFileNameSnapshot = meta.OriginalFileName;
            contentTypeSnapshot = meta.ContentType;
            sizeInBytesSnapshot = meta.SizeInBytes;
        }

        if (r.IsCover)
            await _mediaRepository.UnsetAllCoversAsync(request.VesselId, cancellationToken);

        var media = VesselMediaEntity.Create(
            request.VesselId,
            r.MediaType,
            r.FileId,
            originalFileNameSnapshot,
            contentTypeSnapshot,
            sizeInBytesSnapshot,
            r.SortOrder,
            r.IsCover);

        await _mediaRepository.AddAsync(media, cancellationToken);

        if (r.FileId.HasValue)
        {
            try
            {
                await _fileStorage.LinkFileToVesselOwnerAsync(r.FileId.Value, media.Id, "VesselMedia", accessToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to link file {FileId} to media {MediaId}. Continuing.", r.FileId, media.Id);
            }
        }

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);
        // The user vessel list carries the cover URL (M4f) — evict it so a new/cover photo shows on the list/Home
        // card immediately (the list is keyed by the owner's UserId; the participant edits their own vessel).
        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        await _publisher.PublishAsync(new VesselMediaAddedMessage
        {
            VesselId = request.VesselId,
            MediaId = media.Id,
            FileId = media.FileId,
            IsCover = media.IsCover,
            ActorUserId = currentUserId
        }, cancellationToken);

        if (media.IsCover)
        {
            await _publisher.PublishAsync(new VesselCoverMediaChangedMessage
            {
                VesselId = request.VesselId,
                CoverMediaId = media.Id,
                ActorUserId = currentUserId
            }, cancellationToken);
        }

        return new AddVesselMediaResponse(media.ToDto());
    }
}
