using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;

[DocumentationInfo("ServiceRequest assignment entity", "Assignment of a service request to a specific provider profile after offer acceptance.")]
public sealed class ServiceRequestAssignmentEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ServiceRequestOfferId { get; private set; }
    public long ProviderProfileId { get; private set; }
    public long ProviderUserId { get; private set; }
    public long? AssignedTeamMemberId { get; private set; }
    public ServiceRequestAssignmentStatus Status { get; private set; }
    public DateTime? ScheduledStartDate { get; private set; }
    public DateTime? ScheduledEndDate { get; private set; }
    public DateTime? ActualStartDate { get; private set; }
    public DateTime? ActualEndDate { get; private set; }
    public string? ProviderNotes { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<ServiceRequestWorkLogEntity> _workLogs = new();
    public IReadOnlyCollection<ServiceRequestWorkLogEntity> WorkLogs => _workLogs.AsReadOnly();

    public ServiceRequestAssignmentEntity() { }

    public static ServiceRequestAssignmentEntity Create(
        long serviceRequestId,
        long serviceRequestOfferId,
        long providerProfileId,
        long providerUserId,
        long? assignedTeamMemberId,
        DateTime? scheduledStartDate,
        DateTime? scheduledEndDate)
    {
        return new ServiceRequestAssignmentEntity
        {
            ServiceRequestId = serviceRequestId,
            ServiceRequestOfferId = serviceRequestOfferId,
            ProviderProfileId = providerProfileId,
            ProviderUserId = providerUserId,
            AssignedTeamMemberId = assignedTeamMemberId,
            Status = ServiceRequestAssignmentStatus.Pending,
            ScheduledStartDate = scheduledStartDate,
            ScheduledEndDate = scheduledEndDate,
            IsActive = true
        };
    }

    public void Accept() => Status = ServiceRequestAssignmentStatus.Accepted;

    public void Reject(string? reason)
    {
        Status = ServiceRequestAssignmentStatus.Rejected;
        RejectionReason = reason;
    }

    public void Schedule(DateTime start, DateTime? end)
    {
        Status = ServiceRequestAssignmentStatus.Scheduled;
        ScheduledStartDate = start;
        ScheduledEndDate = end;
    }

    public void Start()
    {
        Status = ServiceRequestAssignmentStatus.InProgress;
        ActualStartDate = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ServiceRequestAssignmentStatus.Completed;
        ActualEndDate = DateTime.UtcNow;
    }

    public void Cancel(string? reason)
    {
        Status = ServiceRequestAssignmentStatus.Cancelled;
        CancellationReason = reason;
    }

    public void AddWorkLog(ServiceRequestWorkLogEntity log) => _workLogs.Add(log);
}
