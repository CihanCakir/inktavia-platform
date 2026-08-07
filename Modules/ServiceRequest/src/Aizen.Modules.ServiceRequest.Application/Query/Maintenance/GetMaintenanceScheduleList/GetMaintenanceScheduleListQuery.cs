using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Query.Maintenance;

[DocumentationInfo("Get maintenance schedule list query", "Lists maintenance schedules (S12), optionally by vessel; includes inactive by default for admin.")]
public sealed class GetMaintenanceScheduleListQuery : AizenQuery<GetMaintenanceScheduleListResponse>
{
    public long? VesselId { get; }

    /// <summary>Admin default: include deactivated schedules so they stay visible/reactivatable.</summary>
    public bool IncludeInactive { get; }

    public GetMaintenanceScheduleListQuery(long? vesselId, bool includeInactive = true)
    {
        VesselId        = vesselId;
        IncludeInactive = includeInactive;
    }
}
