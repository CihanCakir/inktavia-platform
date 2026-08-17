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
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
