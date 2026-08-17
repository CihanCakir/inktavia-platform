
namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest detail DTO", "Full detail DTO with all navigation properties for a service request.")]
public sealed class ServiceRequestDetailDto
{
    public ServiceRequestDto Request { get; set; } = default!;
    public List<ServiceRequestItemDto> Items { get; set; } = new();
    public List<ServiceRequestAttachmentDto> Attachments { get; set; } = new();
    public List<ServiceRequestOfferDto> Offers { get; set; } = new();
    public ServiceRequestAssignmentDto? Assignment { get; set; }

    /// <summary>
    /// Provider work-log entries for the active assignment (evidence trail). Additive read field — surfaced on the
    /// admin detail so the admin panel can view work-log evidence photos (<see cref="ServiceRequestWorkLogDto.AttachmentFileId"/>),
    /// which the separate UI-shaped /work-logs read model drops. Empty when there is no assignment or no logs.
    /// </summary>
    public List<ServiceRequestWorkLogDto> WorkLogs { get; set; } = new();

    public ServiceRequestCompletionDto? Completion { get; set; }
    public ServiceRequestDisputeDto? Dispute { get; set; }
    public List<ServiceRequestStatusHistoryDto> StatusHistory { get; set; } = new();
}
