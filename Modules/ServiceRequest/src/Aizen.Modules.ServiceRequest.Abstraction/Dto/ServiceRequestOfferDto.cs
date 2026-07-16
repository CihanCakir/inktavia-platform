using Aizen.Modules.ServiceRequest.Abstraction.Enum;

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

    // Computed totals
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }

    // Commercial terms
    public OfferDepositType DepositType { get; set; }
    public decimal? DepositValue { get; set; }
    public string? PaymentTermsNote { get; set; }
    public string? WarrantyNote { get; set; }

    // Lifecycle
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ViewedAt { get; set; }

    public List<ServiceRequestOfferItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
