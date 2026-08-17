using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

/// <summary>PUT /api/v1/mobile/maintenance-schedules/{id}/active — activate/deactivate one of the caller's OWN
/// schedules. Owner-gated module-side (the schedule's OwnerUserId must equal the token owner); the reactivate-conflict
/// error surfaces verbatim. Deactivating stops the N2 reminder.</summary>
public sealed class SetMobileMaintenanceScheduleActiveCommand : AizenCommand<MobileMaintenanceSetActiveResultDto>
{
    public SetMobileMaintenanceScheduleActiveCommand(long scheduleId, bool isActive)
    {
        ScheduleId = scheduleId;
        IsActive = isActive;
    }

    public long ScheduleId { get; }
    public bool IsActive { get; }
}
