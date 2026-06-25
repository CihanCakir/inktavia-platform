using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

[DocumentationInfo("Update work phase response", "Result after updating a work phase's progress and status.")]
public sealed class UpdateWorkPhaseResponse(int phaseNumber, int progressPercent, string status)
{
    public int PhaseNumber { get; } = phaseNumber;
    public int ProgressPercent { get; } = progressPercent;
    public string Status { get; } = status;
}
