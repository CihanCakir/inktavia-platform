using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class AddWorkLogEntryAdminCommand : AizenCommand<AddWorkLogEntryResponse>
{
    public long ServiceRequestId { get; }
    public AddWorkLogEntryRequest Payload { get; }
    public string UserToken { get; }

    public AddWorkLogEntryAdminCommand(long serviceRequestId, AddWorkLogEntryRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        UserToken = userToken;
    }
}
