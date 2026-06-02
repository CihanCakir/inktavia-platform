using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Add Vessel Media Command Handler", "Creates a vessel media entity and invalidates media and vessel detail caches.")]
public sealed class AddVesselMediaCommandHandler : AizenCommandHandler<AddVesselMediaCommand, VesselMediaDto>
{
    private readonly IVesselMediaRepository _mediaRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public AddVesselMediaCommandHandler(
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

    public override async Task<VesselMediaDto?> Handle(AddVesselMediaCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        var media = VesselMediaEntity.Create(
            request.VesselId,
            r.MediaType,
            r.FileId,
            r.FileName,
            r.FileUrl,
            r.SortOrder,
            r.IsCover);

        await _mediaRepository.AddAsync(media, cancellationToken);

        await _invalidation.InvalidateMediaAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return media.ToDto();
    }
}
