using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Update Vessel Engine Command Handler", "Loads engine, applies update and invalidates engines cache.")]
public sealed class UpdateVesselEngineCommandHandler : AizenCommandHandler<UpdateVesselEngineCommand, VesselEngineDto>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselEngineCommandHandler(IVesselEngineRepository engineRepository, IVesselCacheInvalidationService invalidation)
    {
        _engineRepository = engineRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselEngineDto?> Handle(UpdateVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var engine = await _engineRepository.GetByIdAsync(request.EngineId, cancellationToken)
            ?? throw new KeyNotFoundException($"Engine {request.EngineId} not found.");

        var r = request.Request;
        engine.Update(r.EngineName, r.EngineTypeCode, r.FuelTypeCode, r.Brand, r.Model, r.SerialNumber, r.HorsePower, r.ProductionYear);
        _engineRepository.Update(engine);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);

        return engine.ToDto();
    }
}
