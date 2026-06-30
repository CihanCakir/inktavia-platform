
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Update service request offer request", "Input model for updating an existing provider offer.")]
public sealed class UpdateServiceRequestOfferRequest
{
    public string? Description { get; set; }
    public string? ProviderNotes { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<CreateServiceRequestOfferItemRequest> Items { get; set; } = new();
}
