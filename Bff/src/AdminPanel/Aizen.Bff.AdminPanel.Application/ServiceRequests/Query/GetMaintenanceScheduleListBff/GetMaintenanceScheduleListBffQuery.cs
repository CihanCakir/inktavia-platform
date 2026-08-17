using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetMaintenanceScheduleListBffQuery : AizenQuery<GetMaintenanceScheduleListResponse>
{
    public long? VesselId { get; }

    /// <summary>Admin default: include deactivated schedules so they stay visible/reactivatable.</summary>
    public bool IncludeInactive { get; }

    public GetMaintenanceScheduleListBffQuery(long? vesselId, bool includeInactive = true)
    {
        VesselId        = vesselId;
        IncludeInactive = includeInactive;
    }
}
