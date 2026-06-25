using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest offer DTO", "Represents a provider offer submitted for a service request.")]
public sealed class ServiceRequestOfferDto
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long ProviderProfileId { get; set; }
    public long ProviderUserId { get; set; }
    public ServiceRequestOfferStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? Description { get; set; }
    public string? ProviderNotes { get; set; }
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<ServiceRequestOfferItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
