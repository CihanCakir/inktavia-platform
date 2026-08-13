using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

/// <summary>GET /api/v1/mobile/maintenance-schedules — the caller-owner's own maintenance schedules (owner-scoped
/// module-side by the asserted identity). Cost-free. Optionally narrowed to one vessel; includeInactive by default.</summary>
public sealed class GetMobileMaintenanceSchedulesQuery : AizenQuery<MobileMaintenanceScheduleListDto>
{
    public GetMobileMaintenanceSchedulesQuery(long? vesselId, bool includeInactive)
    {
        VesselId = vesselId;
        IncludeInactive = includeInactive;
    }

    public long? VesselId { get; }
    public bool IncludeInactive { get; }
}
