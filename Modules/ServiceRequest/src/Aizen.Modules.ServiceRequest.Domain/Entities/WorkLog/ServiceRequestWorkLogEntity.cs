using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;

[DocumentationInfo("ServiceRequest work log entity", "Work progress log entry added by a provider during assignment execution.")]
public sealed class ServiceRequestWorkLogEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ServiceRequestAssignmentId { get; private set; }
    public long ProviderUserId { get; private set; }
    public ServiceRequestWorkLogType LogType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal? LocationLatitude { get; private set; }
    public decimal? LocationLongitude { get; private set; }
    public Guid? AttachmentFileId { get; private set; }
    public DateTime LoggedAt { get; private set; }

    public ServiceRequestWorkLogEntity() { }

    public static ServiceRequestWorkLogEntity Create(
        long serviceRequestId,
        long serviceRequestAssignmentId,
        long providerUserId,
        ServiceRequestWorkLogType logType,
        string title,
        string? description,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? attachmentFileId)
    {
        return new ServiceRequestWorkLogEntity
        {
            ServiceRequestId = serviceRequestId,
            ServiceRequestAssignmentId = serviceRequestAssignmentId,
            ProviderUserId = providerUserId,
            LogType = logType,
            Title = title.Trim(),
            Description = description,
            LocationLatitude = locationLatitude,
            LocationLongitude = locationLongitude,
            AttachmentFileId = attachmentFileId,
            LoggedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
