using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Remove Vessel Media Command Handler", "Deactivates a vessel media item and invalidates media cache.")]
public sealed class RemoveVesselMediaCommandHandler : AizenCommandHandler<RemoveVesselMediaCommand, bool>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public RemoveVesselMediaCommandHandler(IVesselMediaRepository mediaRepository, IVesselCacheInvalidationService invalidation)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(RemoveVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(request.MediaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        media.Deactivate();
        _mediaRepository.Update(media);

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);

        return true;
    }
}
