using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Specification;

[DocumentationInfo("Remove Vessel Specification Command Handler", "Removes vessel specification and invalidates spec and vessel detail caches.")]
public sealed class RemoveVesselSpecificationCommandHandler : AizenCommandHandler<RemoveVesselSpecificationCommand, bool>
{
    private readonly IVesselSpecificationRepository _specRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public RemoveVesselSpecificationCommandHandler(
        IVesselSpecificationRepository specRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _specRepository = specRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<bool> Handle(RemoveVesselSpecificationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var spec = await _specRepository.GetByVesselIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Specification for vessel {request.VesselId} not found.");

        _specRepository.Remove(spec);

        await _invalidation.InvalidateSpecificationAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return true;
    }
}
