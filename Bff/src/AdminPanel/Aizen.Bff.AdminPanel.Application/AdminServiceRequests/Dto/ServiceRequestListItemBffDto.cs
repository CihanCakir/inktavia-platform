using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

[DocumentationInfo("ServiceRequest list item BFF DTO", "Flat list item DTO for admin service request list screen. Avoids exposing module interface types in BFF response contract.")]
public sealed class ServiceRequestListItemBffDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public long VesselId { get; set; }
    public string? ServiceType { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public long OwnerUserId { get; set; }
    public long? ProviderProfileId { get; set; }
    public int OfferCount { get; set; }
    public bool HasActiveAssignment { get; set; }
    public DateTime? RequestedDate { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
