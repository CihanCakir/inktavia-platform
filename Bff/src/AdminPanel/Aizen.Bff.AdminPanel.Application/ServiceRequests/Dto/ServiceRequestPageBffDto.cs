using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("ServiceRequest page BFF DTO", "Concrete pagination wrapper for service request list. Avoids IPaginate interface serialization issues.")]
public sealed class ServiceRequestPageBffDto
{
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public int Count { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public IReadOnlyList<ServiceRequestListItemBffDto> Items { get; set; } = Array.Empty<ServiceRequestListItemBffDto>();
}
