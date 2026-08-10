using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// BE-MO8 — owner-gated activate/deactivate. Loads the schedule and verifies the caller owns it
/// (<c>OwnerUserId == command.OwnerUserId</c>), surfacing a clean not-found on any mismatch (never leaking that a
/// schedule exists). Otherwise applies the same S12 toggle: idempotent no-op when already in the requested state, the
/// reactivate-conflict guard (another active schedule owning the (vessel, category, type) key → clean business error,
/// no 500, no write), and the domain <c>Deactivate()</c>/<c>Reactivate()</c>. The admin set-active is untouched.
/// </summary>
[DocumentationInfo("Set owner maintenance schedule active handler", "Owner-gated activate/deactivate with the reactivate-conflict guard (MO8).")]
public sealed class SetOwnerMaintenanceScheduleActiveCommandHandler
    : AizenCommandHandler<SetOwnerMaintenanceScheduleActiveCommand, SetMaintenanceScheduleActiveResponse>
{
    private readonly IMaintenanceScheduleRepository _repository;

    public SetOwnerMaintenanceScheduleActiveCommandHandler(IMaintenanceScheduleRepository repository)
        => _repository = repository;

    public override async Task<SetMaintenanceScheduleActiveResponse?> Handle(
        SetOwnerMaintenanceScheduleActiveCommand command, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(command.ScheduleId, cancellationToken);

        // Owner gate: clean not-found on unknown id OR a schedule the caller does not own.
        if (schedule is null || command.OwnerUserId <= 0 || schedule.OwnerUserId != command.OwnerUserId)
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
