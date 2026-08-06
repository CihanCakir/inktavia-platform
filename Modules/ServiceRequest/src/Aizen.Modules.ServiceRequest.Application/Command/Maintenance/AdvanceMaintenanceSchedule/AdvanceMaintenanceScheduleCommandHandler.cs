using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Application.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// S12 — when a service request completes, advance the active maintenance schedule matching its (vessel, category,
/// type): stamp LastPerformedAt = the approval time, recompute NextDueAt, re-arm the reminder. No matching schedule →
/// no-op (unless <c>MaintenanceAutoCreateOnCompletion</c> is enabled — default off for MVP). Separate from CargoDry.
/// </summary>
[DocumentationInfo("Advance maintenance schedule handler", "Advances the matching schedule on completion approval (S12).")]
public sealed class AdvanceMaintenanceScheduleCommandHandler
    : AizenCommandHandler<AdvanceMaintenanceScheduleCommand, AdvanceMaintenanceScheduleResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestCompletionRepository _completionRepository;
    private readonly IMaintenanceScheduleRepository _scheduleRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<AdvanceMaintenanceScheduleCommandHandler> _logger;

    public AdvanceMaintenanceScheduleCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestCompletionRepository completionRepository,
        IMaintenanceScheduleRepository scheduleRepository,
        IConfiguration config,
        ILogger<AdvanceMaintenanceScheduleCommandHandler> logger)
    {
        _srRepository         = srRepository;
        _completionRepository = completionRepository;
        _scheduleRepository   = scheduleRepository;
        _config               = config;
        _logger               = logger;
    }

    public override async Task<AdvanceMaintenanceScheduleResponse?> Handle(
        AdvanceMaintenanceScheduleCommand command, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(command.ServiceRequestId, cancellationToken);
        if (sr is null)
            return new AdvanceMaintenanceScheduleResponse { Advanced = false };

        // The approval time (the completion's ReviewedAt) is the "performed" moment; fall back to now.
        var completion  = await _completionRepository.GetByServiceRequestIdAsync(command.ServiceRequestId, cancellationToken);
        var performedAt = completion?.ReviewedAt ?? DateTime.UtcNow;

        var schedule = await _scheduleRepository.GetActiveMatchForCompletionAsync(
            sr.VesselId, sr.ServiceCategoryCode, sr.ServiceTypeCode, cancellationToken);

        if (schedule is not null)
        {
            schedule.RecordPerformed(performedAt);
            _scheduleRepository.Update(schedule);
            _logger.LogInformation(
                "MaintenanceSchedule advanced: schedule {ScheduleId} (vessel {VesselId}, {Category}) → NextDueAt {NextDue}.",
                schedule.Id, sr.VesselId, sr.ServiceCategoryCode, schedule.NextDueAt);
            return new AdvanceMaintenanceScheduleResponse { Advanced = true, ScheduleId = schedule.Id };
        }

        // No schedule for this (vessel, category): no-op unless the config flag opts into auto-create (default off).
        if (MaintenanceScheduleOptions.AutoCreateOnCompletion(_config))
        {
            var created = MaintenanceScheduleEntity.Create(
                sr.VesselId, sr.ServiceCategoryCode, sr.ServiceTypeCode, sr.OwnerUserId,
                MaintenanceScheduleOptions.DefaultInterval(_config),
                MaintenanceScheduleOptions.DefaultLeadDays(_config),
                lastPerformedAt: performedAt,
                notes: "Auto-created on completion.");
            await _scheduleRepository.AddAsync(created, cancellationToken);
            _logger.LogInformation(
                "MaintenanceSchedule auto-created for vessel {VesselId} category {Category} on completion.",
                sr.VesselId, sr.ServiceCategoryCode);
            return new AdvanceMaintenanceScheduleResponse { Advanced = true, ScheduleId = created.Id };
        }

        return new AdvanceMaintenanceScheduleResponse { Advanced = false };
    }
}
