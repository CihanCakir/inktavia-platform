using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

[DocumentationInfo("Upsert maintenance schedule command", "Create or update a recurring maintenance schedule (S12).")]
public sealed class UpsertMaintenanceScheduleCommand : AizenCommand<UpsertMaintenanceScheduleResponse>
{
    public UpsertMaintenanceScheduleRequest Request { get; }
    public UpsertMaintenanceScheduleCommand(UpsertMaintenanceScheduleRequest request) => Request = request;
}
