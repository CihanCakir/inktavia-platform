using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Update Vessel Media Command Handler", "Loads media, applies metadata update and invalidates media cache.")]
public sealed class UpdateVesselMediaCommandHandler : AizenCommandHandler<UpdateVesselMediaCommand, VesselMediaDto>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselMediaCommandHandler(
        IVesselMediaRepository mediaRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _mediaRepository = mediaRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<VesselMediaDto?> Handle(UpdateVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var media = await _mediaRepository.GetByIdAsync(request.MediaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        media.Update(request.Request.SortOrder, request.Request.IsCover);
        _mediaRepository.Update(media);

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);

        return media.ToDto();
    }
}
