using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

public sealed class SaveOfferDraftRequest
{
    public string CurrencyCode { get; set; } = "TRY";
    public string? Description { get; set; }
    public string? ProviderNotes { get; set; }
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<CreateServiceRequestOfferItemRequest> Items { get; set; } = new();

    // Commercial terms
    public OfferDepositType DepositType { get; set; }
    public decimal? DepositValue { get; set; }
    public string? PaymentTermsNote { get; set; }
    public string? WarrantyNote { get; set; }

    /// <summary>Concurrency token from the last read. Null on first save (create).</summary>
    public string? ConcurrencyToken { get; set; }
}
