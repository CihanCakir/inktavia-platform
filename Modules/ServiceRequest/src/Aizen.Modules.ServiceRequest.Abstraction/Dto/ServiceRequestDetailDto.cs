using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest detail DTO", "Full detail DTO with all navigation properties for a service request.")]
public sealed class ServiceRequestDetailDto
{
    public ServiceRequestDto Request { get; set; } = default!;
    public List<ServiceRequestItemDto> Items { get; set; } = new();
    public List<ServiceRequestAttachmentDto> Attachments { get; set; } = new();
    public List<ServiceRequestOfferDto> Offers { get; set; } = new();
    public ServiceRequestAssignmentDto? Assignment { get; set; }
    public ServiceRequestCompletionDto? Completion { get; set; }
    public ServiceRequestDisputeDto? Dispute { get; set; }
    public List<ServiceRequestStatusHistoryDto> StatusHistory { get; set; } = new();
}
