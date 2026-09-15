namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// Full detail aggregate for a provider viewing a service request.
/// Privacy-filtered: no OwnerUserId, snapped coordinates, only the caller's own offer.
/// </summary>
public sealed class ProviderServiceRequestDetailDto
{
    public ProviderServiceRequestDto Request { get; set; } = default!;
    public List<WorkScopeItemDto> WorkScope { get; set; } = new();
    public List<ProviderAttachmentMetaDto> Attachments { get; set; } = new();
    public ServiceRequestOfferDto? MyOffer { get; set; }
    /// <summary>The calling provider's assignment (job) id when the request is assigned to them; null otherwise.
    /// Lets the detail page link an accepted+assigned request straight to its Job.</summary>
    public long? AssignmentId { get; set; }
    public List<ServiceRequestStatusHistoryDto> Timeline { get; set; } = new();
    public int OfferCount { get; set; }
    public int AttachmentCount { get; set; }
    /// <summary>CargoDry supply v2: product media block for a CARGODRY_SUPPLY request (name + presigned image URLs).
    /// Populated by the provider BFF from FileStorage; null for non-supply requests. The module never sets it.</summary>
    public CargoDrySupplyProductBlockDto? CargoDryProduct { get; set; }
}

/// <summary>
/// Provider-facing product media block for a CARGODRY_SUPPLY request. All fields are BFF-resolved (presigned URLs);
/// the ServiceRequest module leaves this null.
/// </summary>
public sealed class CargoDrySupplyProductBlockDto
{
    public string        ProductCode  { get; set; } = default!;
    public string?       ProductName  { get; set; }
    public decimal?      RetailPrice  { get; set; }
    public string?       CurrencyCode { get; set; }
    public string?       ThumbnailUrl { get; set; }
    public List<string>  ImageUrls    { get; set; } = new();
}
