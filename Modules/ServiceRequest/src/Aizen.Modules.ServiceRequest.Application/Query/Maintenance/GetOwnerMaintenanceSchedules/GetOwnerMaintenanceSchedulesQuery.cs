using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Query.Maintenance;

/// <summary>
/// BE-MO8 — the caller-owner's own maintenance schedules (OwnerUserId-scoped), optionally narrowed to one vessel,
/// includeInactive so deactivated schedules stay visible/reactivatable. The owner id comes from the trusted context
/// (BFF assertion), never a parameter. Reuses the S12 list response.
/// </summary>
[DocumentationInfo("Get owner maintenance schedules query", "Lists the caller-owner's own maintenance schedules. Identity from the trusted context.")]
public sealed class GetOwnerMaintenanceSchedulesQuery : AizenQuery<GetMaintenanceScheduleListResponse>
{
    public long OwnerUserId { get; }
    public long? VesselId { get; }
    public bool IncludeInactive { get; }

    public GetOwnerMaintenanceSchedulesQuery(long ownerUserId, long? vesselId = null, bool includeInactive = true)
    {
        OwnerUserId     = ownerUserId;
        VesselId        = vesselId;
        IncludeInactive = includeInactive;
    }
}
