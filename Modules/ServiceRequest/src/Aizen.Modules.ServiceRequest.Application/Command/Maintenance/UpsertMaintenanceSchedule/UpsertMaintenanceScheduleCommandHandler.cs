using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// S12 — idempotent upsert of a maintenance schedule by its unique key (VesselId, ServiceCategoryCode, ServiceTypeCode)
/// among active rows. An existing active schedule is updated (never a second active created — the duplicate-active
/// guard); otherwise a new one is created. Interval/lead default from <see cref="MaintenanceScheduleOptions"/> when
/// the request omits them. Flushes so the generated id + computed NextDueAt are returned.
/// </summary>
[DocumentationInfo("Upsert maintenance schedule handler", "Creates or updates the per-vessel, per-category schedule (S12).")]
public sealed class UpsertMaintenanceScheduleCommandHandler
    : AizenCommandHandler<UpsertMaintenanceScheduleCommand, UpsertMaintenanceScheduleResponse>
{
    private readonly IMaintenanceScheduleRepository _repository;
    private readonly IConfiguration _config;

    public UpsertMaintenanceScheduleCommandHandler(
        IMaintenanceScheduleRepository repository, IConfiguration config)
    {
        _repository = repository;
        _config     = config;
    }

    public override async Task<UpsertMaintenanceScheduleResponse?> Handle(
        UpsertMaintenanceScheduleCommand command, CancellationToken cancellationToken)
    {
        var r = command.Request;

        if (r.VesselId <= 0 || r.OwnerUserId <= 0 || string.IsNullOrWhiteSpace(r.ServiceCategoryCode))
            throw new AizenBusinessException(
                "A maintenance schedule needs a vessel, an owner and a service category.");

        var interval = r.RecommendedIntervalMonths ?? MaintenanceScheduleOptions.DefaultInterval(_config);
        var lead     = r.ReminderLeadDays ?? MaintenanceScheduleOptions.DefaultLeadDays(_config);
        if (interval < 1)
            throw new AizenBusinessException("RecommendedIntervalMonths must be at least 1.");
        if (lead < 0)
            throw new AizenBusinessException("ReminderLeadDays cannot be negative.");

        var categoryCode = r.ServiceCategoryCode.Trim();
        var typeCode     = string.IsNullOrWhiteSpace(r.ServiceTypeCode) ? null : r.ServiceTypeCode.Trim();

        var existing = await _repository.GetActiveByKeyAsync(r.VesselId, categoryCode, typeCode, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateSchedule(interval, lead, r.Notes);
            if (r.LastPerformedAt is { } backfill)
                existing.RecordPerformed(backfill);   // optional backfill re-anchors the cycle
            _repository.Update(existing);
            await _repository.SaveChangesAsync(cancellationToken);
            return new UpsertMaintenanceScheduleResponse(existing.Id, created: false, existing.NextDueAt);
        }

        var entity = MaintenanceScheduleEntity.Create(
            r.VesselId, categoryCode, typeCode, r.OwnerUserId, interval, lead, r.LastPerformedAt, r.Notes);
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);   // flush to populate the identity id + persist NextDueAt

        return new UpsertMaintenanceScheduleResponse(entity.Id, created: true, entity.NextDueAt);
    }
}
