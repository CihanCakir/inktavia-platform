using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Query.Maintenance;

[DocumentationInfo("Get maintenance schedule list query", "Lists active maintenance schedules (S12), optionally by vessel.")]
public sealed class GetMaintenanceScheduleListQuery : AizenQuery<GetMaintenanceScheduleListResponse>
{
    public long? VesselId { get; }
    public GetMaintenanceScheduleListQuery(long? vesselId) => VesselId = vesselId;
}
