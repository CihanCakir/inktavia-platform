using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class UpdateWorkPhaseBffCommand : AizenCommand<UpdateWorkPhaseResponse>
{
    public long ServiceRequestId { get; }
    public int PhaseNumber { get; }
    public UpdateWorkPhaseRequest Payload { get; }

    public UpdateWorkPhaseBffCommand(long serviceRequestId, int phaseNumber, UpdateWorkPhaseRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        PhaseNumber = phaseNumber;
        Payload = payload;
    }
}
