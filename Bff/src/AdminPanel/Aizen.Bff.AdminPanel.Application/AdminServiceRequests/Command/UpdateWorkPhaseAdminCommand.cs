using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class UpdateWorkPhaseAdminCommand : AizenCommand<UpdateWorkPhaseResponse>
{
    public long ServiceRequestId { get; }
    public int PhaseNumber { get; }
    public UpdateWorkPhaseRequest Payload { get; }
    public string UserToken { get; }

    public UpdateWorkPhaseAdminCommand(long serviceRequestId, int phaseNumber, UpdateWorkPhaseRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        PhaseNumber = phaseNumber;
        Payload = payload;
        UserToken = userToken;
    }
}
