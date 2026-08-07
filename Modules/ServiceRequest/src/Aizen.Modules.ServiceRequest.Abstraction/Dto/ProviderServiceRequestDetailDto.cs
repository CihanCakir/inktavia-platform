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
}
