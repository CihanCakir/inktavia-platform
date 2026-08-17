using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class AddWorkLogEntryBffCommand : AizenCommand<AddWorkLogEntryResponse>
{
    public long ServiceRequestId { get; }
    public AddWorkLogEntryRequest Payload { get; }

    public AddWorkLogEntryBffCommand(long serviceRequestId, AddWorkLogEntryRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
