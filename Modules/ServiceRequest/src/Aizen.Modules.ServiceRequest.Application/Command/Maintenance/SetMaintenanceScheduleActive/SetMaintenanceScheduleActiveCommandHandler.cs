using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// S12 — reachable deactivate/reactivate for a maintenance schedule. Loads the schedule, toggles
/// <c>IsActive</c> via the domain <c>Deactivate()</c>/<c>Reactivate()</c>, and persists.
///
/// <para>Idempotent: setting the state it already has is a no-op (no write). Not-found is a clean business
/// error. Reactivating is guarded — the (VesselId, ServiceCategoryCode, ServiceTypeCode) key has a
/// partial-unique-active index, so turning a schedule back on while <b>another</b> active schedule owns the
/// same key would violate it; that is caught up front and surfaced as a clean business error instead of a
/// 500. Deactivating never triggers the guard (it frees the slot).</para>
/// </summary>
[DocumentationInfo("Set maintenance schedule active handler", "Activate/deactivate a schedule with the reactivate-conflict guard (S12).")]
public sealed class SetMaintenanceScheduleActiveCommandHandler
    : AizenCommandHandler<SetMaintenanceScheduleActiveCommand, SetMaintenanceScheduleActiveResponse>
{
    private readonly IMaintenanceScheduleRepository _repository;

    public SetMaintenanceScheduleActiveCommandHandler(IMaintenanceScheduleRepository repository)
        => _repository = repository;

    public override async Task<SetMaintenanceScheduleActiveResponse?> Handle(
        SetMaintenanceScheduleActiveCommand command, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(command.ScheduleId, cancellationToken);
        if (schedule is null)
            throw new AizenBusinessException("Maintenance schedule not found.");

        // Idempotent: already in the requested state → no write.
        if (schedule.IsActive == command.IsActive)
            return new SetMaintenanceScheduleActiveResponse(schedule.Id, schedule.IsActive);

        if (command.IsActive)
        {
            // Reactivate guard: another active schedule must not already own this (vessel, category, type) key.
            var activeForKey = await _repository.GetActiveByKeyAsync(
                schedule.VesselId, schedule.ServiceCategoryCode, schedule.ServiceTypeCode, cancellationToken);
            if (activeForKey is not null && activeForKey.Id != schedule.Id)
                throw new AizenBusinessException(
                    "Another active maintenance schedule already exists for this vessel and category. " +
                    "Deactivate it first, then reactivate this one.");

            schedule.Reactivate();
        }
        else
        {
            schedule.Deactivate();
        }

        _repository.Update(schedule);
        await _repository.SaveChangesAsync(cancellationToken);

        return new SetMaintenanceScheduleActiveResponse(schedule.Id, schedule.IsActive);
    }
}
