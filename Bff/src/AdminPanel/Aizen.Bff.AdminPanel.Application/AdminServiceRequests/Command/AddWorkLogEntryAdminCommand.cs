using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class AddWorkLogEntryAdminCommand : AizenCommand<AddWorkLogEntryResponse>
{
    public long ServiceRequestId { get; }
    public AddWorkLogEntryRequest Payload { get; }

    public AddWorkLogEntryAdminCommand(long serviceRequestId, AddWorkLogEntryRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
