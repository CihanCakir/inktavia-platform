using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Add Vessel Engine Command Handler", "Creates a vessel engine entity and invalidates engines and vessel detail caches.")]
public sealed class AddVesselEngineCommandHandler : AizenCommandHandler<AddVesselEngineCommand, AddVesselEngineResponse>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public AddVesselEngineCommandHandler(
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

    public override async Task<AddVesselEngineResponse?> Handle(AddVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var r = request.Request;

        if (!string.IsNullOrWhiteSpace(r.SerialNumber))
        {
            var existing = await _engineRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
            if (existing.Any(e => string.Equals(e.SerialNumber, r.SerialNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An engine with serial number '{r.SerialNumber}' already exists for this vessel.");
        }

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
        engine.SetBrandModel(r.EngineBrandId, r.EngineModelId);

        await _engineRepository.AddAsync(engine, cancellationToken);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return new AddVesselEngineResponse(engine.ToDto());
    }
}
