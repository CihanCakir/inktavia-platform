using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Remove Vessel Media Command Handler", "Deactivates a vessel media item, invalidates media cache and publishes VesselMediaRemovedMessage.")]
public sealed class RemoveVesselMediaCommandHandler : AizenCommandHandler<RemoveVesselMediaCommand, RemoveVesselMediaResponse>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;

    public RemoveVesselMediaCommandHandler(
        IVesselMediaRepository mediaRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _publisher = publisher;
        _info = info;
    }

    public override async Task<RemoveVesselMediaResponse?> Handle(RemoveVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles.Contains("Admin");

        var media = await _mediaRepository.GetByIdWithVesselAsync(request.MediaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(media.VesselId, currentUserId, cancellationToken);

        media.Deactivate();
        _mediaRepository.Update(media);

        await _invalidation.InvalidateMediaAsync(media.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselMediaRemovedMessage
        {
            VesselId = media.VesselId,
            MediaId = media.Id,
            ActorUserId = currentUserId
        }, cancellationToken);

        return new RemoveVesselMediaResponse(media.VesselId, media.Id);
    }
}
