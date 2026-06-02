using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Set Cover Vessel Media Command Handler", "Clears cover flag on all vessel media, sets it on target and invalidates media and vessel detail caches.")]
public sealed class SetCoverVesselMediaCommandHandler : AizenCommandHandler<SetCoverVesselMediaCommand, bool>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public SetCoverVesselMediaCommandHandler(IVesselMediaRepository mediaRepository, IVesselCacheInvalidationService invalidation)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(SetCoverVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var allMedia = await _mediaRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);

        foreach (var item in allMedia)
        {
            if (item.IsCover)
            {
                item.ClearCover();
                _mediaRepository.Update(item);
            }
        }

        var target = allMedia.FirstOrDefault(m => m.Id == request.MediaId)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        target.SetCover();
        _mediaRepository.Update(target);

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return true;
    }
}
