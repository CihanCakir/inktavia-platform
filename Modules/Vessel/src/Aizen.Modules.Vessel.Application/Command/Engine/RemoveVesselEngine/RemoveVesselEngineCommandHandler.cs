using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Remove Vessel Engine Command Handler", "Deactivates a vessel engine and invalidates engines cache.")]
public sealed class RemoveVesselEngineCommandHandler : AizenCommandHandler<RemoveVesselEngineCommand, RemoveVesselEngineResponse>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public RemoveVesselEngineCommandHandler(
        IVesselEngineRepository engineRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _engineRepository = engineRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<RemoveVesselEngineResponse?> Handle(RemoveVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var engine = await _engineRepository.GetByIdAsync(request.EngineId, cancellationToken)
            ?? throw new KeyNotFoundException($"Engine {request.EngineId} not found.");

        engine.Deactivate();
        _engineRepository.Update(engine);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);

        return new RemoveVesselEngineResponse(request.VesselId, request.EngineId);
    }
}
