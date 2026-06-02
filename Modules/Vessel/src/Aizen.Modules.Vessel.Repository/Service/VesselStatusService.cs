using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel status service", "Validates and applies vessel status transitions, recording history entries.")]
public sealed class VesselStatusService : IVesselStatusService
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselStatusHistoryRepository _historyRepository;

    public VesselStatusService(IVesselRepository vesselRepository, IVesselStatusHistoryRepository historyRepository)
    {
        _vesselRepository = vesselRepository;
        _historyRepository = historyRepository;
    }

    public async Task ChangeStatusAsync(long vesselId, VesselStatus newStatus, string? reason, long? changedByUserId, CancellationToken ct = default)
    {
        var vessel = await _vesselRepository.GetByIdAsync(vesselId, ct)
            ?? throw new KeyNotFoundException($"Vessel {vesselId} not found.");

        if (!IsValidTransition(vessel.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from {vessel.Status} to {newStatus}.");

        var previousStatus = vessel.Status;
        vessel.ChangeStatus(newStatus);
        _vesselRepository.Update(vessel);

        var history = VesselStatusHistoryEntity.Create(vesselId, previousStatus, newStatus, reason, changedByUserId);
        await _historyRepository.AddAsync(history, ct);
    }

    public bool IsValidTransition(VesselStatus from, VesselStatus to) =>
        (from, to) switch
        {
            (VesselStatus.Draft, VesselStatus.Active) => true,
            (VesselStatus.Draft, VesselStatus.Passive) => true,
            (VesselStatus.Active, VesselStatus.Passive) => true,
            (VesselStatus.Active, VesselStatus.UnderMaintenance) => true,
            (VesselStatus.Passive, VesselStatus.Active) => true,
            (VesselStatus.UnderMaintenance, VesselStatus.Active) => true,
            (VesselStatus.UnderMaintenance, VesselStatus.Passive) => true,
            _ => false
        };
}
