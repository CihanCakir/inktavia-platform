using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// Provider-safe projection of a service request. No OwnerUserId, no raw coordinates.
/// </summary>
public sealed class ProviderServiceRequestDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ServiceRequestStatus Status { get; set; }
    public ServiceRequestPriority Priority { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? ApproxLatitude { get; set; }
    public decimal? ApproxLongitude { get; set; }
    public decimal? DistanceKm { get; set; }
    // Phase-1 quote helpers (populated by the provider BFF from the provider profile, not the SR module):
    // the provider's default per-km rate and the suggested travel fee (distanceKm × ratePerKm) the portal
    // pre-fills as an editable "Yol bedeli / Travel fee" line.
    public decimal? RatePerKm { get; set; }
    public decimal? SuggestedTravelFee { get; set; }
    public long VesselId { get; set; }
    public string? VesselName { get; set; }
    public string? VesselTypeCode { get; set; }
    public string? VesselBrand { get; set; }
    public string? VesselModel { get; set; }
    public decimal? VesselLengthValue { get; set; }
    public string? VesselLengthUnitCode { get; set; }
    public int? VesselYear { get; set; }
    public string? VesselMaterialCode { get; set; }
    public string? VesselRegistrationNumber { get; set; }
    public decimal? VesselBeamValue { get; set; }
    public string? VesselBeamUnitCode { get; set; }
    public decimal? VesselDraftValue { get; set; }
    public string? VesselDraftUnitCode { get; set; }
    public string? OwnerNotes { get; set; }
    /// <summary>CargoDry supply flow: requested product code (non-null only for CARGODRY_SUPPLY). Drives the fixed retail price the provider accepts.</summary>
    public string? CargoDryProductCode { get; set; }
    /// <summary>CargoDry supply flow: whether the calling provider may accept this request. True for non-CargoDry requests. Set by the provider BFF.</summary>
    public bool CanAccept { get; set; } = true;
    /// <summary>CargoDry supply flow: reason the provider cannot accept (null when CanAccept). Set by the provider BFF.</summary>
    public string? CanAcceptReason { get; set; }
    /// <summary>CargoDry supply flow: whether the owner has marked the calling provider as their preferred CargoDry supplier. Set by the provider BFF.</summary>
    public bool IsPreferred { get; set; }
    /// <summary>CargoDry supply v2 (A1): the calling provider's available consignment-kit count for the requested product (null for non-CARGODRY_SUPPLY). Set by the provider BFF.</summary>
    public int? AvailableKitCount { get; set; }
    /// <summary>CargoDry supply v2: when the provider marked the kit delivered (null until delivered). Lets the portal render the delivered state from server data, not only its own action.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
    /// <summary>CargoDry supply v2: deadline after which delivery auto-completes the order (set at ship/deliver time; null otherwise).</summary>
    public DateTime? AutoCompleteDeadlineUtc { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
