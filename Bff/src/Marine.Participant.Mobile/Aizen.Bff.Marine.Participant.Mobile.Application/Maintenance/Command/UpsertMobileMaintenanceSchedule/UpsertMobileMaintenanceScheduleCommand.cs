using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

/// <summary>POST /api/v1/mobile/maintenance-schedules — create/edit a schedule for one of the caller's OWN vessels.
/// Idempotent by (vessel, category, type). The BFF enforces vessel-ownership (the caller may only schedule their own
/// vessels); OwnerUserId is stamped from the token module-side (the body never carries it).</summary>
public sealed class UpsertMobileMaintenanceScheduleCommand : AizenCommand<MobileMaintenanceUpsertResultDto>
{
    public UpsertMobileMaintenanceScheduleCommand(MobileUpsertMaintenanceScheduleRequest request) => Request = request;
    public MobileUpsertMaintenanceScheduleRequest Request { get; }
}
