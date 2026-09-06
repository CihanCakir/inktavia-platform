using Aizen.Modules.ServiceRequest.Abstraction.Enum;

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

    // ── Phase-1 distance pricing — BFF-INJECTED (resolved from the provider profile, not the client) ──────────
    /// <summary>Provider FIXED business location; the SR module computes distanceKm = Haversine(center ↔ SR job
    /// location) server-side and snapshots it. Null → distanceKm null (null-safe).</summary>
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    /// <summary>Provider default per-km rate; seeds the optional suggested "Yol bedeli / Travel fee" line.</summary>
    public decimal? RatePerKm { get; set; }
    /// <summary>Opt-out for the suggested travel line. Null/true = add it when a rate + distance exist and the
    /// provider didn't already submit a Travel line; false = never add (provider removed it).</summary>
    public bool? IncludeSuggestedTravelFee { get; set; }
}
