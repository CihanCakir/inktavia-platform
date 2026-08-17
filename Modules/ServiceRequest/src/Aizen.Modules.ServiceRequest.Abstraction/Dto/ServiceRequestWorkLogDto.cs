using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest work log DTO", "Represents a provider work log entry attached to an assignment.")]
public sealed class ServiceRequestWorkLogDto
{
    public long Id { get; set; }
    public long ServiceRequestAssignmentId { get; set; }
    public long ProviderUserId { get; set; }
    public ServiceRequestWorkLogType LogType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public Guid? AttachmentFileId { get; set; }
    public DateTime LoggedAt { get; set; }
}
