using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;

[DocumentationInfo("Work phase entity", "A phase in the service request work progress timeline.")]
public sealed class WorkPhaseEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public int PhaseNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int ProgressPercent { get; private set; }
    public string Status { get; private set; } = "Upcoming";
    public int DisplayOrder { get; private set; }

    public WorkPhaseEntity() { }

    public static WorkPhaseEntity Create(long serviceRequestId, int phaseNumber, string title, int displayOrder)
    {
        return new WorkPhaseEntity
        {
            ServiceRequestId = serviceRequestId,
            PhaseNumber = phaseNumber,
            Title = title.Trim(),
            ProgressPercent = 0,
            Status = "Upcoming",
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void UpdateProgress(int percent, string status)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
        Status = status;
    }
}
