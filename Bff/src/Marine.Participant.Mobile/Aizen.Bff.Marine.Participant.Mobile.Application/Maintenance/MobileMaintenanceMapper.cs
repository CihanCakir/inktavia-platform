using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

/// <summary>
/// Projects the S12 maintenance DTOs → the cost-free mobile owner contracts (BE_MO8). Drops the OwnerUserId echo and
/// the N2 ReminderSentAt marker; everything else is customer-facing schedule data.
/// </summary>
internal static class MobileMaintenanceMapper
{
    public static MobileMaintenanceScheduleDto MapSchedule(MaintenanceScheduleDto s) => new()
    {
        Id                        = s.Id,
        VesselId                  = s.VesselId,
        ServiceCategoryCode       = s.ServiceCategoryCode,
        ServiceTypeCode           = s.ServiceTypeCode,
        RecommendedIntervalMonths = s.RecommendedIntervalMonths,
        ReminderLeadDays          = s.ReminderLeadDays,
        LastPerformedAt           = s.LastPerformedAt,
        NextDueAt                 = s.NextDueAt,
        IsActive                  = s.IsActive,
        Notes                     = s.Notes,
    };

    public static MobileMaintenanceScheduleListDto MapList(GetMaintenanceScheduleListResponse? r) => new()
    {
        Schedules = r?.Schedules?.Select(MapSchedule).ToList() ?? new List<MobileMaintenanceScheduleDto>(),
    };

    public static MobileMaintenanceUpsertResultDto MapUpsertResult(UpsertMaintenanceScheduleResponse r) => new()
    {
        ScheduleId = r.ScheduleId,
        Created    = r.Created,
        NextDueAt  = r.NextDueAt,
    };

    public static MobileMaintenanceSetActiveResultDto MapSetActiveResult(SetMaintenanceScheduleActiveResponse r) => new()
    {
        ScheduleId = r.ScheduleId,
        IsActive   = r.IsActive,
    };
}
