using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Create service request offer request", "Input model for a provider to create an offer.")]
public sealed class CreateServiceRequestOfferRequest
{
    public long ProviderProfileId { get; set; }
    public string? Description { get; set; }
    public string? ProviderNotes { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<CreateServiceRequestOfferItemRequest> Items { get; set; } = new();
}
