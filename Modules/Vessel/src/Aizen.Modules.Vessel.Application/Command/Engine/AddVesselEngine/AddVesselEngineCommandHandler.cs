using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Add Vessel Engine Command Handler", "Creates a vessel engine entity and invalidates engines and vessel detail caches.")]
public sealed class AddVesselEngineCommandHandler : AizenCommandHandler<AddVesselEngineCommand, VesselEngineDto>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public AddVesselEngineCommandHandler(IVesselEngineRepository engineRepository, IVesselCacheInvalidationService invalidation)
    {
        _engineRepository = engineRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselEngineDto?> Handle(AddVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var engine = VesselEngineEntity.Create(
            request.VesselId,
            r.EngineName,
            r.EngineTypeCode,
            r.FuelTypeCode,
            r.Brand,
            r.Model,
            r.SerialNumber,
            r.HorsePower,
            r.ProductionYear,
            r.IsPrimary);

        await _engineRepository.AddAsync(engine, cancellationToken);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return engine.ToDto();
    }
}
