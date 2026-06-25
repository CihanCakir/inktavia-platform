using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Update work phase command", "Updates the progress percent and status of a specific work phase.")]
public sealed class UpdateWorkPhaseCommand : AizenCommand<UpdateWorkPhaseResponse>
{
    public long ServiceRequestId { get; }
    public int PhaseNumber { get; }
    public UpdateWorkPhaseRequest Request { get; }

    public UpdateWorkPhaseCommand(long serviceRequestId, int phaseNumber, UpdateWorkPhaseRequest request)
    {
        ServiceRequestId = serviceRequestId;
        PhaseNumber = phaseNumber;
        Request = request;
    }
}
