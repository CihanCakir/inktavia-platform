using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest DTO", "Core DTO representing a service request summary with status and priority.")]
public sealed class ServiceRequestDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public long OwnerUserId { get; set; }
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ServiceRequestStatus Status { get; set; }
    public ServiceRequestPriority Priority { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public DateTime? ExpiresAt { get; set; }
    /// <summary>CargoDry supply flow: requested product code (non-null only for CARGODRY_SUPPLY).</summary>
    public string? CargoDryProductCode { get; set; }

    // ── CargoDry supply v2 — owner-facing order view (non-null only for CARGODRY_SUPPLY) ───────────────────────
    /// <summary>Derived owner order status: OrderReceived | ProviderAssigned | AwaitingShipment | Shipped | Delivered | Completed | Cancelled.</summary>
    public string? CargoDryOrderStatus { get; set; }
    /// <summary>Assigned provider display name (ProviderAssigned/Delivered states).</summary>
    public string? CargoDryProviderName { get; set; }
    public DateTime? CargoDryProviderAcceptDeadlineUtc { get; set; }
    public DateTime? CargoDryDeliveredAtUtc { get; set; }
    public DateTime? CargoDryShippedAtUtc { get; set; }
    /// <summary>Cargo (direct online sale) carrier tracking code (Shipped state).</summary>
    public string? CargoDryTrackingCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
