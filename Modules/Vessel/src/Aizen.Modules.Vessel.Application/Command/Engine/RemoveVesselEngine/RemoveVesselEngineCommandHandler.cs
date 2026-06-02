using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Remove Vessel Engine Command Handler", "Deactivates a vessel engine and invalidates engines cache.")]
public sealed class RemoveVesselEngineCommandHandler : AizenCommandHandler<RemoveVesselEngineCommand, bool>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public RemoveVesselEngineCommandHandler(IVesselEngineRepository engineRepository, IVesselCacheInvalidationService invalidation)
    {
        _engineRepository = engineRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(RemoveVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var engine = await _engineRepository.GetByIdAsync(request.EngineId, cancellationToken)
            ?? throw new KeyNotFoundException($"Engine {request.EngineId} not found.");

        engine.Deactivate();
        _engineRepository.Update(engine);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);

        return true;
    }
}
