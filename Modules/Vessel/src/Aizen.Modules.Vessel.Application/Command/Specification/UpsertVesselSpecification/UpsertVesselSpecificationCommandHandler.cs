using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Specification;

[DocumentationInfo("Upsert Vessel Specification Command Handler", "Creates or updates vessel specification and invalidates spec and vessel detail caches.")]
public sealed class UpsertVesselSpecificationCommandHandler : AizenCommandHandler<UpsertVesselSpecificationCommand, VesselSpecificationDto>
{
    private readonly IVesselSpecificationRepository _specRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpsertVesselSpecificationCommandHandler(
        IVesselSpecificationRepository specRepository,
        IVesselCacheInvalidationService invalidation)
    {
        _specRepository = specRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselSpecificationDto?> Handle(UpsertVesselSpecificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _specRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        var r = request.Request;

        VesselSpecificationEntity spec;
        if (existing is null)
        {
            spec = VesselSpecificationEntity.Create(
                request.VesselId,
                r.Brand, r.Model, r.ProductionYear,
                r.LengthValue, r.LengthUnitCode,
                r.BeamValue, r.BeamUnitCode,
                r.DraftValue, r.DraftUnitCode,
                r.WeightValue, r.WeightUnitCode,
                r.CabinCount, r.BedCount, r.BathroomCount,
                r.HullMaterialCode,
                r.FuelCapacityValue, r.FuelCapacityUnitCode,
                r.WaterCapacityValue, r.WaterCapacityUnitCode);
            await _specRepository.AddAsync(spec, cancellationToken);
        }
        else
        {
            existing.Update(
                r.Brand, r.Model, r.ProductionYear,
                r.LengthValue, r.LengthUnitCode,
                r.BeamValue, r.BeamUnitCode,
                r.DraftValue, r.DraftUnitCode,
                r.WeightValue, r.WeightUnitCode,
                r.CabinCount, r.BedCount, r.BathroomCount,
                r.HullMaterialCode,
                r.FuelCapacityValue, r.FuelCapacityUnitCode,
                r.WaterCapacityValue, r.WaterCapacityUnitCode);
            _specRepository.Update(existing);
            spec = existing;
        }

        await _invalidation.InvalidateSpecificationAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return spec.ToDto();
    }
}
