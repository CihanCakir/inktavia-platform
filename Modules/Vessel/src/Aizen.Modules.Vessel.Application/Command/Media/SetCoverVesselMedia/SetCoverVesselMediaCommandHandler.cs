using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Set Cover Vessel Media Command Handler", "Clears cover flag on all vessel media, sets it on target, invalidates media and vessel detail caches and publishes VesselCoverMediaChangedMessage.")]
public sealed class SetCoverVesselMediaCommandHandler : AizenCommandHandler<SetCoverVesselMediaCommand, SetCoverVesselMediaResponse>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;

    public SetCoverVesselMediaCommandHandler(
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

    public override async Task<SetCoverVesselMediaResponse?> Handle(SetCoverVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles.Contains("Admin");

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        await _mediaRepository.UnsetAllCoversAsync(request.VesselId, cancellationToken);

        var allMedia = await _mediaRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        var target = allMedia.FirstOrDefault(m => m.Id == request.MediaId)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        target.SetCover();
        _mediaRepository.Update(target);

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselCoverMediaChangedMessage
        {
            VesselId = request.VesselId,
            CoverMediaId = request.MediaId,
            ActorUserId = currentUserId
        }, cancellationToken);

        return new SetCoverVesselMediaResponse(request.VesselId, request.MediaId);
    }
}
