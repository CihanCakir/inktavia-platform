using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Set Primary Vessel Engine Command Handler", "Clears primary flag on all vessel engines, sets it on target and invalidates engines cache.")]
public sealed class SetPrimaryVesselEngineCommandHandler : AizenCommandHandler<SetPrimaryVesselEngineCommand, bool>
{
    private readonly IVesselEngineRepository _engineRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public SetPrimaryVesselEngineCommandHandler(
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

    public override async Task<bool> Handle(SetPrimaryVesselEngineCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var engines = await _engineRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);

        foreach (var engine in engines)
        {
            if (engine.IsPrimary)
            {
                engine.ClearPrimary();
                _engineRepository.Update(engine);
            }
        }

        var target = engines.FirstOrDefault(e => e.Id == request.EngineId)
            ?? throw new KeyNotFoundException($"Engine {request.EngineId} not found.");

        target.SetPrimary();
        _engineRepository.Update(target);

        await _invalidation.InvalidateEnginesAsync(request.VesselId, cancellationToken);

        return true;
    }
}
