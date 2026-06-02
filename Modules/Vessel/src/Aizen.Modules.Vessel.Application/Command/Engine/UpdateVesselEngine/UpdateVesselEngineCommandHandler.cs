using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Update Vessel Engine Command Handler", "Loads engine, applies update and invalidates engines cache.")]
public sealed class UpdateVesselEngineCommandHandler : AizenCommandHandler<UpdateVesselEngineCommand, UpdateVesselEngineResponse>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselEngineCommandHandler(
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

    public override async Task<UpdateVesselEngineResponse?> Handle(UpdateVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var engine = await _engineRepository.GetByIdAsync(request.EngineId, cancellationToken)
            ?? throw new KeyNotFoundException($"Engine {request.EngineId} not found.");

        var r = request.Request;
        engine.Update(r.EngineName, r.EngineTypeCode, r.FuelTypeCode, r.Brand, r.Model, r.SerialNumber, r.HorsePower, r.ProductionYear);
        _engineRepository.Update(engine);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);

        return new UpdateVesselEngineResponse(engine.ToDto());
    }
}
